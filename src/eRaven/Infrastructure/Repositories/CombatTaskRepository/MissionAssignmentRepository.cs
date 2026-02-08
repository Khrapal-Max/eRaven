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
    /// <inheritdoc />
    public async Task<IReadOnlyList<ActiveMissionPersonDto>> GetActiveByMissionAsync(
        Guid missionId,
        DateOnly onDate,
        CancellationToken ct = default)
    {
        if (missionId == Guid.Empty)
            throw new ArgumentException("MissionId is required.", nameof(missionId));
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // 1) Active на дату = open-ended (To == null) + вже стартувало (From <= onDate)
        var assignments = await db.MissionAssignments
            .AsNoTracking()
            .Where(a => a.MissionId == missionId)
            .Where(a => a.To == null)
            .Where(a => a.From <= onDate)
            .Select(a => new { a.PersonId, a.From })
            .ToListAsync(ct);

        if (assignments.Count == 0)
            return [];

        // Якщо раптом є дублікати open-ended по PersonId — беремо останній старт (max From)
        var active = assignments
            .GroupBy(x => x.PersonId)
            .Select(g => g.OrderByDescending(x => x.From).First())
            .OrderBy(x => x.From)
            .ThenBy(x => x.PersonId)
            .ToList();

        var personIds = active.Select(x => x.PersonId).ToList();

        // 2) Добираємо персон одним запитом
        var persons = await db.PersonRead
            .AsNoTracking()
            .Where(p => personIds.Contains(p.Id))
            .ToListAsync(ct);

        var map = persons.ToDictionary(x => x.Id);

        // 3) Склеюємо у DTO в потрібному порядку
        var result = new List<ActiveMissionPersonDto>(active.Count);

        foreach (var a in active)
        {
            if (!map.TryGetValue(a.PersonId, out var p))
                continue; // або throw, якщо PersonRead гарантується

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

    /// <inheritdoc />
    public async Task<MissionAssignment?> GetActiveForPersonAsync(
        Guid personId,
        DateOnly onDate,
        CancellationToken ct = default)
    {
        if (personId == Guid.Empty) throw new ArgumentException("PersonId is required.", nameof(personId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.MissionAssignments
            .AsNoTracking()
            .Where(x => x.PersonId == personId)
            .Where(x => x.From <= onDate && (!x.To.HasValue || x.To.Value >= onDate))
            .OrderByDescending(x => x.From)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MissionAssignment>> GetPersonAssignmentsAsync(
        Guid personId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        if (personId == Guid.Empty) throw new ArgumentException("PersonId is required.", nameof(personId));
        if (to < from) throw new ArgumentException("To must be >= From.", nameof(to));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.MissionAssignments
            .AsNoTracking()
            .Where(x => x.PersonId == personId)
            .Where(x => x.From <= to && (!x.To.HasValue || x.To.Value >= from)) // overlap
            .OrderBy(x => x.From)
            .ThenBy(x => x.MissionId)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReadyCombatTaskPersonDto>> GetFreePersonForMissionsAsync(
        DateOnly onDate,
        CancellationToken ct = default)
    {
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Active = open-ended на дату (як у тебе).
        // Якщо треба "активні на дату" з To>=onDate — легко допрацювати.
        var activePersonIds = db.MissionAssignments
            .AsNoTracking()
            .Where(a => a.From <= onDate && a.To == null)
            .Select(a => a.PersonId);

        return await db.PersonRead
            .AsNoTracking()
            .Where(p => !activePersonIds.Contains(p.Id))
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

    /// <summary>
    /// Застосовує Posted-рядки (Start/End) до <see cref="MissionAssignment"/>.
    /// Важливо: в межах однієї дати для однієї особи спочатку обробляємо End, потім Start.
    /// </summary>
    /// <inheritdoc />
    public async Task ApplyPostedLinesAsync(
        IReadOnlyList<CombatTaskPostedDetailsDto> lines,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(lines, nameof(lines));
        if (lines.Count == 0) return;

        ValidatePosted(lines);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Детермінований порядок:
        // EffectiveAt ASC, End before Start, PersonId, MissionId
        var ordered = lines
            .OrderBy(x => x.EffectiveAt)
            .ThenBy(x => x.Kind == CombatTaskDetailsKind.End ? 0 : 1)
            .ThenBy(x => x.PersonId)
            .ThenBy(x => x.MissionId)
            .ToList();

        // Ключовий момент: підтягуємо всі open-ended для залучених осіб ОДИН РАЗ (tracked).
        // Це дозволяє враховувати "закриття" (To=...) до SaveChanges().
        var personIds = ordered.Select(x => x.PersonId).Distinct().ToList();

        var openByPersons = await db.MissionAssignments
            .Where(x => personIds.Contains(x.PersonId))
            .Where(x => x.To == null)
            .OrderByDescending(x => x.From)
            .ToListAsync(ct);

        foreach (var line in ordered)
        {
            if (line.Kind == CombatTaskDetailsKind.Start)
            {
                // Забороняємо паралельні open-ended (людина одночасно лише в одній місії)
                var openAny = openByPersons
                    .FirstOrDefault(x => x.PersonId == line.PersonId && x.To == null);

                if (openAny is not null)
                    throw new InvalidOperationException(
                        $"Неможливо відкрити місію: особа вже має активну місію (MissionId={openAny.MissionId}).");

                var a = new MissionAssignment
                {
                    Id = Guid.NewGuid(),
                    PersonId = line.PersonId,
                    MissionId = line.MissionId,
                    From = line.EffectiveAt,
                    To = null,
                    SourceStartDocumentId = line.DocumentId,
                    SourceStartDetailsId = line.DetailsId
                };

                db.MissionAssignments.Add(a);
                openByPersons.Add(a);
            }
            else // End
            {
                // Закриваємо саме ту місію, що в рядку.
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
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static void ValidatePosted(IReadOnlyList<CombatTaskPostedDetailsDto> lines)
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
