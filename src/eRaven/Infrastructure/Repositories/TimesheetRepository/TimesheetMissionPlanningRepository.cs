//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetMissionPlanningRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// EF Core реалізація <see cref="ITimesheetMissionPlanningRepository"/>.
/// </summary>
public sealed class TimesheetMissionPlanningRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetMissionPlanningRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReadyCombatTaskPersonDto>> GetFreePersonForMissionsAsync(
        DateOnly onDate,
        CancellationToken ct = default)
    {
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // In timesheet on date = EXISTS active episode on date
        // Busy on date = EXISTS active span on date (Draft або Posted), тобто Status != Canceled
        var query = db.PersonRead
            .AsNoTracking()
            .Where(p => db.TimeSheets
                .AsNoTracking()
                .Any(t =>
                    t.PersonId == p.Id &&
                    t.OpenedAt <= onDate &&
                    (!t.ClosedAt.HasValue || t.ClosedAt.Value >= onDate)))
            .Where(p => !db.TimesheetTaskSpans
                .AsNoTracking()
                .Any(s =>
                    s.PersonId == p.Id &&
                    s.FromDate <= onDate &&
                    (!s.ToDate.HasValue || s.ToDate.Value >= onDate) &&
                    s.Status != DocumentStatus.Canceled))
            // ✅ OrderBy ДО Select, щоб SQLite не ламався на DTO-проекції
            .OrderBy(p => p.FullName)
            .ThenBy(p => p.Rnokpp)
            .Select(p => new ReadyCombatTaskPersonDto(
                PersonId: p.Id,
                Rnokpp: p.Rnokpp,
                FullName: p.FullName,
                Rank: p.Rank,
                Position: p.Position,
                Weapon: p.Weapon,
                Callsign: p.Callsign));

        return await query.ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ActiveMissionPersonDto>> GetActiveByMissionAsync(
        Guid missionId,
        DateOnly onDate,
        bool includeDraft,
        CancellationToken ct = default)
    {
        if (missionId == Guid.Empty)
            throw new ArgumentException("MissionId is required.", nameof(missionId));
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var allowed = includeDraft
            ? [DocumentStatus.Draft, DocumentStatus.Posted]
            : new[] { DocumentStatus.Posted };

        var spans = await db.TimesheetTaskSpans
            .AsNoTracking()
            .Where(s => s.MissionId == missionId)
            .Where(s => allowed.Contains(s.Status))
            .Where(s => s.Status != DocumentStatus.Canceled)
            .Where(s => s.FromDate <= onDate && (!s.ToDate.HasValue || s.ToDate.Value >= onDate))
            .Select(s => new { s.CombatTaskDocumentId, s.MissionId, s.PersonId, s.FromDate })
            .ToListAsync(ct);

        if (spans.Count == 0)
            return [];

        var ids = spans.Select(x => x.PersonId).Distinct().ToList();

        var persons = await db.PersonRead
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(ct);

        var map = persons.ToDictionary(x => x.Id);

        // 1 активний span на людину (інваріант табеля), але на всяк випадок беремо найсвіжіший From
        return [.. spans
            .GroupBy(x => x.PersonId)
            .Select(g => g.OrderByDescending(x => x.FromDate).First())
            .OrderBy(x => x.FromDate)
            .ThenBy(x => x.PersonId)
            .Select(x =>
            {
                var p = map[x.PersonId];
                return new ActiveMissionPersonDto(
                    CombatTaskDocumentId: x.CombatTaskDocumentId,
                    MissionId: x.MissionId,
                    PersonId: p.Id,
                    Rnokpp: p.Rnokpp,
                    FullName: p.FullName,
                    Callsign: p.Callsign,
                    Rank: p.Rank,
                    Position: p.Position,
                    Weapon: p.Weapon,
                    From: x.FromDate);
            })];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ActiveMissionPersonDto>> GetActiveByDocumentAsync(
        Guid documentId,
        DateOnly onDate,
        bool includeDraft,
        CancellationToken ct = default)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var allowed = includeDraft
            ? [DocumentStatus.Draft, DocumentStatus.Posted]
            : new[] { DocumentStatus.Posted };

        var spans = await db.TimesheetTaskSpans
            .AsNoTracking()
            .Where(s => s.CombatTaskDocumentId == documentId)
            .Where(s => allowed.Contains(s.Status))
            .Where(s => s.Status != DocumentStatus.Canceled)
            .Where(s => s.FromDate <= onDate && (!s.ToDate.HasValue || s.ToDate.Value >= onDate))
            .Select(s => new { s.CombatTaskDocumentId, s.MissionId, s.PersonId, s.FromDate })
            .ToListAsync(ct);

        if (spans.Count == 0)
            return [];

        var ids = spans.Select(x => x.PersonId).Distinct().ToList();

        var persons = await db.PersonRead
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(ct);

        var map = persons.ToDictionary(x => x.Id);

        return [.. spans
            .GroupBy(x => x.PersonId)
            .Select(g => g.OrderByDescending(x => x.FromDate).First())
            .OrderBy(x => x.FromDate)
            .ThenBy(x => x.PersonId)
            .Select(x =>
            {
                var p = map[x.PersonId];
                return new ActiveMissionPersonDto(
                    CombatTaskDocumentId: x.CombatTaskDocumentId,
                    MissionId: x.MissionId,
                    PersonId: p.Id,
                    Rnokpp: p.Rnokpp,
                    FullName: p.FullName,
                    Callsign: p.Callsign,
                    Rank: p.Rank,
                    Position: p.Position,
                    Weapon: p.Weapon,
                    From: x.FromDate);
            })];
    }
}
