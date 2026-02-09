//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionAssignmentRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// EF Core реалізація <see cref="IMissionAssignmentRepository"/>.
/// </summary>
public sealed class MissionAssignmentRepository(IDbContextFactory<AppDbContext> dbFactory)
    : IMissionAssignmentRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    //======================================================================
    // Reads
    //======================================================================

    public async Task<IReadOnlyList<ActiveMissionPersonDto>> GetActiveByMissionAsync(
        Guid missionId,
        DateOnly onDate,
        bool includePlanned = false,
        CancellationToken ct = default)
    {
        if (missionId == Guid.Empty)
            throw new ArgumentException("MissionId is required.", nameof(missionId));
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.MissionAssignments.AsNoTracking();

        q = FilterByStatus(q, includePlanned);

        // Active на дату = open-ended (To == null) + вже стартувало (From <= onDate)
        var assignments = await q
            .Where(a => a.MissionId == missionId)
            .Where(a => a.To == null)
            .Where(a => a.From <= onDate)
            .Select(a => new { a.PersonId, a.From })
            .ToListAsync(ct);

        if (assignments.Count == 0)
            return [];

        var active = assignments
            .GroupBy(x => x.PersonId)
            .Select(g => g.OrderByDescending(x => x.From).First())
            .OrderBy(x => x.From)
            .ThenBy(x => x.PersonId)
            .ToList();

        var personIds = active.Select(x => x.PersonId).ToList();

        var persons = await db.PersonRead
            .AsNoTracking()
            .Where(p => personIds.Contains(p.Id))
            .ToListAsync(ct);

        var map = persons.ToDictionary(x => x.Id);

        var result = new List<ActiveMissionPersonDto>(active.Count);
        foreach (var a in active)
        {
            if (!map.TryGetValue(a.PersonId, out var p))
                continue;

            result.Add(new ActiveMissionPersonDto(
                PersonId: p.Id,
                Rnokpp: p.Rnokpp,
                FullName: p.FullName,
                Callsign: p.Callsign,
                Rank: p.Rank,
                Position: p.Position,
                Weapon: p.Weapon,
                From: a.From
            ));
        }

        return result;
    }

    public async Task<MissionAssignment?> GetActiveForPersonAsync(
        Guid personId,
        DateOnly onDate,
        bool includePlanned = false,
        CancellationToken ct = default)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.MissionAssignments.AsNoTracking();
        q = FilterByStatus(q, includePlanned);

        return await q
            .Where(x => x.PersonId == personId)
            .Where(x => x.From <= onDate && (!x.To.HasValue || x.To.Value >= onDate))
            .OrderByDescending(x => x.From)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<MissionAssignment>> GetPersonAssignmentsAsync(
        Guid personId,
        DateOnly from,
        DateOnly to,
        bool includePlanned = false,
        CancellationToken ct = default)
    {
        if (personId == Guid.Empty) throw new ArgumentException("PersonId is required.", nameof(personId));
        if (to < from) throw new ArgumentException("To must be >= From.", nameof(to));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.MissionAssignments.AsNoTracking();
        q = FilterByStatus(q, includePlanned);

        return await q
            .Where(x => x.PersonId == personId)
            .Where(x => x.From <= to && (!x.To.HasValue || x.To.Value >= from)) // overlap
            .OrderBy(x => x.From)
            .ThenBy(x => x.MissionId)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ReadyCombatTaskPersonDto>> GetFreePersonForMissionsAsync(
        DateOnly onDate,
        bool includePlanned = false,
        CancellationToken ct = default)
    {
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Вільні на дату = НЕ мають призначення, яке покриває onDate
        // (це ближче до вашого “код 30 блокує якщо є assignment на дату”).
        var active = db.MissionAssignments.AsNoTracking();
        active = FilterByStatus(active, includePlanned);

        var busyPersonIds = active
            .Where(a => a.From <= onDate && (!a.To.HasValue || a.To.Value >= onDate))
            .Select(a => a.PersonId);

        return await db.PersonRead
            .AsNoTracking()
            .Where(p => !busyPersonIds.Contains(p.Id))
            .Select(p => new ReadyCombatTaskPersonDto(
                PersonId: p.Id,
                Rnokpp: p.Rnokpp,
                FullName: p.FullName,
                Rank: p.Rank,
                Position: p.Position,
                Weapon: p.Weapon,
                Callsign: p.Callsign
            ))
            .ToListAsync(ct);
    }

    //======================================================================
    // Write (apply posted)
    //======================================================================

    public async Task ApplyDraftLinesAsync(IReadOnlyList<ApplyCombatTaskDetailsDto> lines,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0) return;

        ValidatePosted(lines);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var ordered = lines
            .OrderBy(x => x.EffectiveAt)
            .ThenBy(x => x.Kind == CombatTaskDetailsKind.End ? 0 : 1) // End before Start
            .ThenBy(x => x.PersonId)
            .ThenBy(x => x.MissionId)
            .ToList();

        var personIds = ordered.Select(x => x.PersonId).Distinct().ToList();

        // Витягуємо open-ended для перевірки конфліктів (Committed + Planned)
        var openAll = await db.MissionAssignments
            .Where(x => personIds.Contains(x.PersonId))
            .Where(x => x.To == null)
            .Where(x => x.Status == MissionAssignmentStatus.Planned || x.Status == MissionAssignmentStatus.Committed)
            .OrderByDescending(x => x.From)
            .ToListAsync(ct);

        // Витягуємо існуючі Start-записи по DetailsId (для ідемпотентності)
        var startDetailsIds = ordered
            .Where(x => x.Kind == CombatTaskDetailsKind.Start)
            .Select(x => x.DetailsId)
            .Distinct()
            .ToList();

        var existingStarts = startDetailsIds.Count == 0
            ? []
            : await db.MissionAssignments
                .Where(x => startDetailsIds.Contains(x.SourceStartDetailsId))
                .ToListAsync(ct);

        var byStartDetailsId = existingStarts
            .GroupBy(x => x.SourceStartDetailsId)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var line in ordered)
        {
            if (line.Kind == CombatTaskDetailsKind.Start)
            {
                // Якщо такий старт вже існує:
                if (byStartDetailsId.TryGetValue(line.DetailsId, out var existing))
                {
                    // Якщо вже Committed — draft нічого не змінює (не даунгрейдимо)
                    if (existing.Status == MissionAssignmentStatus.Committed)
                        continue;

                    // existing — Planned: оновимо поля (ідемпотентно)
                    // Конфлікт: інший open-ended (Committed/Planned) по цій особі
                    var otherOpen = openAll.FirstOrDefault(x =>
                        x.PersonId == line.PersonId &&
                        x.To == null &&
                        x.Id != existing.Id);

                    if (otherOpen is not null)
                        throw new InvalidOperationException("Неможливо запланувати: особа вже має активну місію.");

                    existing.From = line.EffectiveAt;
                    existing.To = null;

                    existing.MissionId = line.MissionId;
                    existing.PersonId = line.PersonId;

                    existing.SourceStartDocumentId = line.DocumentId;
                    existing.SourceStartDetailsId = line.DetailsId;
                    existing.SourceEndDocumentId = null;
                    existing.SourceEndDetailsId = null;

                    existing.Status = MissionAssignmentStatus.Planned;

                    if (!openAll.Any(x => x.Id == existing.Id))
                        openAll.Add(existing);

                    continue;
                }

                // Новий Planned Start
                var openAny = openAll.FirstOrDefault(x => x.PersonId == line.PersonId && x.To == null);
                if (openAny is not null)
                    throw new InvalidOperationException("Неможливо запланувати: особа вже має активну місію.");

                var a = new MissionAssignment
                {
                    Id = Guid.NewGuid(),
                    PersonId = line.PersonId,
                    MissionId = line.MissionId,
                    From = line.EffectiveAt,
                    To = null,

                    Status = MissionAssignmentStatus.Planned,

                    SourceStartDocumentId = line.DocumentId,
                    SourceStartDetailsId = line.DetailsId
                };

                db.MissionAssignments.Add(a);
                openAll.Add(a);
            }
            else // End
            {
                // Draft закриває ТІЛЬКИ Planned open-ended.
                var openPlanned = openAll
                    .Where(x => x.PersonId == line.PersonId)
                    .Where(x => x.MissionId == line.MissionId)
                    .Where(x => x.To == null)
                    .Where(x => x.Status == MissionAssignmentStatus.Planned)
                    .OrderByDescending(x => x.From)
                    .FirstOrDefault()
                    ?? throw new InvalidOperationException("Неможливо закрити: немає відкритого планового призначення.");

                if (line.EffectiveAt < openPlanned.From)
                    throw new InvalidOperationException("Неможливо закрити раніше старту.");

                openPlanned.To = line.EffectiveAt;
                openPlanned.SourceEndDocumentId = line.DocumentId;
                openPlanned.SourceEndDetailsId = line.DetailsId;
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    public async Task ApplyPostedLinesAsync(
        IReadOnlyList<ApplyCombatTaskDetailsDto> lines,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(lines, nameof(lines));
        if (lines.Count == 0) return;

        ValidatePosted(lines);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var ordered = lines
            .OrderBy(x => x.EffectiveAt)
            .ThenBy(x => x.Kind == CombatTaskDetailsKind.End ? 0 : 1)
            .ThenBy(x => x.PersonId)
            .ThenBy(x => x.MissionId)
            .ToList();

        var personIds = ordered.Select(x => x.PersonId).Distinct().ToList();

        // Підтягуємо всі open-ended для залучених осіб (tracked), включаючи Planned,
        // бо Posted може “підтвердити” вже запланований старт.
        var openByPersons = await db.MissionAssignments
            .Where(x => personIds.Contains(x.PersonId))
            .Where(x => x.To == null)
            .Where(x => x.Status == MissionAssignmentStatus.Planned || x.Status == MissionAssignmentStatus.Committed)
            .OrderByDescending(x => x.From)
            .ToListAsync(ct);

        // Для Start: швидко знаходимо існуючий assignment по SourceStartDetailsId (tracked)
        var startDetailsIds = ordered
            .Where(x => x.Kind == CombatTaskDetailsKind.Start)
            .Select(x => x.DetailsId)
            .Distinct()
            .ToList();

        var existingStarts = startDetailsIds.Count == 0
            ? []
            : await db.MissionAssignments
                .Where(x => startDetailsIds.Contains(x.SourceStartDetailsId))
                .ToListAsync(ct);

        var byStartDetailsId = existingStarts
            .GroupBy(x => x.SourceStartDetailsId)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var line in ordered)
        {
            if (line.Kind == CombatTaskDetailsKind.Start)
            {
                // Якщо вже є запис з цим старт-рядком — робимо “promotion” Planned->Committed (або idempotent).
                if (byStartDetailsId.TryGetValue(line.DetailsId, out var existing))
                {
                    // Захист від “інший Person/Mission під тим же DetailsId” (не має статись)
                    if (existing.PersonId != line.PersonId || existing.MissionId != line.MissionId)
                        throw new InvalidOperationException(
                            $"Конфлікт ідемпотентності Start: DetailsId={line.DetailsId} уже прив’язаний до іншої особи/місії.");

                    // Не дозволяємо паралельні open-ended (враховуючи Planned теж), але ігноруємо самого existing
                    var openAny = openByPersons
                        .FirstOrDefault(x => x.PersonId == line.PersonId && x.To == null && x.Id != existing.Id);

                    if (openAny is not null)
                        throw new InvalidOperationException(
                            $"Неможливо відкрити місію: особа вже має активну місію (MissionId={openAny.MissionId}).");

                    // Нормалізація (на випадок “старого planned”)
                    existing.From = line.EffectiveAt;
                    existing.To = null;
                    existing.SourceStartDocumentId = line.DocumentId;
                    existing.SourceStartDetailsId = line.DetailsId;
                    existing.SourceEndDocumentId = null;
                    existing.SourceEndDetailsId = null;

                    existing.Status = MissionAssignmentStatus.Committed;

                    if (!openByPersons.Any(x => x.Id == existing.Id))
                        openByPersons.Add(existing);

                    continue;
                }

                // Новий Start
                var openAnyNew = openByPersons
                    .FirstOrDefault(x => x.PersonId == line.PersonId && x.To == null);

                if (openAnyNew is not null)
                    throw new InvalidOperationException(
                        $"Неможливо відкрити місію: особа вже має активну місію (MissionId={openAnyNew.MissionId}).");

                var a = new MissionAssignment
                {
                    Id = Guid.NewGuid(),
                    PersonId = line.PersonId,
                    MissionId = line.MissionId,
                    From = line.EffectiveAt,
                    To = null,
                    Status = MissionAssignmentStatus.Committed,
                    SourceStartDocumentId = line.DocumentId,
                    SourceStartDetailsId = line.DetailsId
                };

                db.MissionAssignments.Add(a);
                openByPersons.Add(a);
            }
            else // End
            {
                var open = openByPersons
                    .Where(x => x.PersonId == line.PersonId)
                    .Where(x => x.MissionId == line.MissionId)
                    .Where(x => x.To == null)
                    .OrderByDescending(x => x.From)
                    .FirstOrDefault()
                    ?? throw new InvalidOperationException(
                        $"Неможливо закрити місію: немає відкритого призначення для PersonId={line.PersonId} MissionId={line.MissionId}.");

                if (line.EffectiveAt < open.From)
                    throw new InvalidOperationException(
                        $"Неможливо закрити місію раніше старту: From={open.From:yyyy-MM-dd}, End={line.EffectiveAt:yyyy-MM-dd}.");

                open.To = line.EffectiveAt;
                open.SourceEndDocumentId = line.DocumentId;
                open.SourceEndDetailsId = line.DetailsId;

                // якщо закриваємо Planned — це теж підтвердження (Committed)
                if (open.Status == MissionAssignmentStatus.Planned)
                    open.Status = MissionAssignmentStatus.Committed;
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static IQueryable<MissionAssignment> FilterByStatus(IQueryable<MissionAssignment> q, bool includePlanned)
    {
        if (includePlanned)
        {
            return q.Where(a =>
                a.Status == MissionAssignmentStatus.Committed ||
                a.Status == MissionAssignmentStatus.Planned);
        }

        return q.Where(a => a.Status == MissionAssignmentStatus.Committed);
    }

    private static void ValidatePosted(IReadOnlyList<ApplyCombatTaskDetailsDto> lines)
    {
        foreach (var line in lines)
        {
            if (line.DocumentId == Guid.Empty) throw new InvalidOperationException("Posted має порожній DocumentId.");
            if (line.CombatTaskId == Guid.Empty) throw new InvalidOperationException("Posted має порожній CombatTaskId.");
            if (line.DetailsId == Guid.Empty) throw new InvalidOperationException("Posted має порожній DetailsId.");
            if (line.PersonId == Guid.Empty) throw new InvalidOperationException("Posted має порожній PersonId.");
            if (line.MissionId == Guid.Empty) throw new InvalidOperationException("Posted має порожній MissionId.");
            if (line.EffectiveAt == default) throw new InvalidOperationException("Posted має некоректну EffectiveAt дату.");
        }
    }
}