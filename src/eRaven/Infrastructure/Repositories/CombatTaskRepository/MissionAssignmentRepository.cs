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

    //======================================================================
    // Write (apply draft and posted)
    //======================================================================

    public async Task ApplyDraftCombatTaskDocumentAsync(
    IReadOnlyList<ApplyCombatTaskDetailsDto> taskDetails,
    CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(taskDetails);
        if (taskDetails.Count == 0) return;

        ValidateApply(taskDetails); // краще перейменувати на ValidateApply(...)

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var tasks = taskDetails
            .OrderBy(x => x.EffectiveAt)
            .ThenBy(x => x.Kind == CombatTaskDetailsKind.End ? 0 : 1) // End before Start
            .ThenBy(x => x.PersonId)
            .ThenBy(x => x.MissionId)
            .ToList();

        var personIds = tasks.Select(t => t.PersonId).Distinct().ToList();

        // Беремо open-ended по задіяним особам (і Planned, і Committed — щоб блокувати Start поверх факту)
        var openTasks = await db.MissionAssignments
            .Where(x => x.To == null)
            .Where(x => personIds.Contains(x.PersonId))
            .Where(x => x.Status == MissionAssignmentStatus.Planned || x.Status == MissionAssignmentStatus.Committed)
            .ToListAsync(ct);

        foreach (var task in tasks)
        {
            if (task.Kind == CombatTaskDetailsKind.End)
            {
                // Draft має право закривати тільки Planned (факт не чіпаємо)
                var open = openTasks
                    .Where(x => x.Status == MissionAssignmentStatus.Planned)
                    .Where(x => x.PersonId == task.PersonId)
                    .Where(x => x.MissionId == task.MissionId)
                    .FirstOrDefault()
                    ?? throw new InvalidOperationException("Неможливо закрити: немає відкритого планового призначення.");

                if (task.EffectiveAt < open.From)
                    throw new InvalidOperationException("Неможливо закрити раніше старту.");

                open.To = task.EffectiveAt;
                open.SourceEndDocumentId = task.DocumentId;
                open.SourceEndDetailsId = task.DetailsId;

                // інтервал більше не open-ended → прибираємо з кешу, щоб не впливав далі
                openTasks.Remove(open);
            }
            else // Start
            {
                // 1 open-ended per person => беремо єдиний
                var open = openTasks.FirstOrDefault(x => x.PersonId == task.PersonId && x.To == null);

                if (open is not null)
                {
                    // Ідемпотентність: це той самий старт-рядок -> оновлюємо Planned
                    if (open.SourceStartDetailsId == task.DetailsId)
                    {
                        // якщо раптом це вже факт (Committed) — просто ігноруємо (Draft не чіпає факт)
                        if (open.Status == MissionAssignmentStatus.Committed)
                            continue;

                        open.MissionId = task.MissionId;
                        open.From = task.EffectiveAt;
                        open.To = null;

                        open.SourceStartDocumentId = task.DocumentId;
                        open.SourceStartDetailsId = task.DetailsId;

                        open.SourceEndDocumentId = null;
                        open.SourceEndDetailsId = null;

                        open.Status = MissionAssignmentStatus.Planned;
                        continue;
                    }

                    // інший open-ended (план або факт) => конфлікт
                    throw new InvalidOperationException("Неможливо запланувати: особа вже має активну місію.");
                }

                // немає open-ended => створюємо новий Planned
                var a = new MissionAssignment
                {
                    Id = Guid.NewGuid(),
                    PersonId = task.PersonId,
                    MissionId = task.MissionId,
                    From = task.EffectiveAt,
                    To = null,

                    Status = MissionAssignmentStatus.Planned,

                    SourceStartDocumentId = task.DocumentId,
                    SourceStartDetailsId = task.DetailsId
                };

                db.MissionAssignments.Add(a);
                openTasks.Add(a); // важливо для наступних рядків в тому ж батчі
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ApplyPostedCombatTaskDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(documentId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var planned = await db.MissionAssignments
            .Where(a => a.Status == MissionAssignmentStatus.Planned)
            .Where(a => a.SourceStartDocumentId == documentId || a.SourceEndDocumentId == documentId)
            .ToListAsync(ct);

        foreach (var a in planned)
            a.Status = MissionAssignmentStatus.Committed;

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

    private static void ValidateApply(IReadOnlyList<ApplyCombatTaskDetailsDto> taskDetails)
    {
        foreach (var detail in taskDetails)
        {
            if (detail.DocumentId == Guid.Empty) throw new InvalidOperationException("Погодженні завдання мають порожній DocumentId.");
            if (detail.CombatTaskId == Guid.Empty) throw new InvalidOperationException("Погодженні завдання мають порожній CombatTaskId.");
            if (detail.DetailsId == Guid.Empty) throw new InvalidOperationException("Погодженні завдання мають порожній DetailsId.");
            if (detail.PersonId == Guid.Empty) throw new InvalidOperationException("Погодженні завдання мають порожній PersonId.");
            if (detail.MissionId == Guid.Empty) throw new InvalidOperationException("Погодженні завдання мають порожній MissionId.");
            if (detail.EffectiveAt == default) throw new InvalidOperationException("Погодженні завдання мають некоректну EffectiveAt дату.");
        }
    }
}