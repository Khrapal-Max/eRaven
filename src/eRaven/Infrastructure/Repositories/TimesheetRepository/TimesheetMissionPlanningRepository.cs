//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetMissionPlanningRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Read-репозиторій для планування/звітів по місіях.
///
/// <para>
/// Джерело правди по завданнях — CombatTask (через матеріалізовані призначення <c>MissionAssignment</c>).
/// Табель використовується лише як додатковий фільтр поточного стану (30/100) для UX.
/// </para>
/// </summary>
public sealed class TimesheetMissionPlanningRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetMissionPlanningRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetActiveMissionPersonsAsync(
        Guid missionId,
        DateOnly onDate,
        CancellationToken ct = default)
    {
        if (missionId == Guid.Empty)
            throw new ArgumentException("missionId must be set.", nameof(missionId));
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Active on date: From <= D && (To is null || D < To)
        var q =
            from a in db.MissionAssignments.AsNoTracking()
            join doc in db.CombatTaskDocuments.AsNoTracking() on a.SourceStartDocumentId equals doc.Id
            where a.MissionId == missionId
            where doc.Status != DocumentStatus.Canceled
            where a.From <= onDate
            where a.To == null || onDate < a.To.Value
            select a.PersonId;

        return await q.Distinct().OrderBy(x => x).ToListAsync(ct);
    }



    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, DateOnly>> GetActiveMissionPersonFromDatesAsync(
        Guid missionId,
        DateOnly onDate,
        IReadOnlyCollection<Guid> personIds,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(personIds);
        if (missionId == Guid.Empty)
            throw new ArgumentException("missionId must be set.", nameof(missionId));
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));
        if (personIds.Count == 0)
            return new Dictionary<Guid, DateOnly>();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Active on date: From <= D && (To is null || D < To)
        var rows = await (
            from a in db.MissionAssignments.AsNoTracking()
            join doc in db.CombatTaskDocuments.AsNoTracking() on a.SourceStartDocumentId equals doc.Id
            where a.MissionId == missionId
            where doc.Status != DocumentStatus.Canceled
            where personIds.Contains(a.PersonId)
            where a.From <= onDate
            where a.To == null || onDate < a.To.Value
            select new { a.PersonId, a.From })
            .ToListAsync(ct);

        return rows
            .GroupBy(x => x.PersonId)
            .ToDictionary(g => g.Key, g => g.Min(x => x.From));
    }
    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetActiveMissionPersonsByDocumentAsync(
        Guid documentId,
        DateOnly onDate,
        CancellationToken ct = default)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("documentId must be set.", nameof(documentId));
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q =
            from a in db.MissionAssignments.AsNoTracking()
            join doc in db.CombatTaskDocuments.AsNoTracking() on a.SourceStartDocumentId equals doc.Id
            where a.SourceStartDocumentId == documentId
            where doc.Status != DocumentStatus.Canceled
            where a.From <= onDate
            where a.To == null || onDate < a.To.Value
            select a.PersonId;

        return await q.Distinct().OrderBy(x => x).ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetFreePersonForMissionsAsync(
        DateOnly onDate,
        CancellationToken ct = default)
    {
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Allowed codes for assignment UX.
        var allowedCodeIds = await db.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.Code == TimesheetSystemCodes.ReadyToCombatTask || x.Code == TimesheetSystemCodes.DoesTheCombatTask)
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (allowedCodeIds.Count == 0)
            return [];

        // Persons with current code in {30,100}.
        // Note: current code is taken from the active episode's entry for the given date.
        var eligibleByCodeQuery = db.TimeSheets
            .AsNoTracking()
            .Where(t => t.OpenedAt <= onDate && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= onDate))
            .Select(t => new
            {
                t.PersonId,
                CurrentCodeId = t.Entries
                    .Where(e => !e.IsDeleted && e.From <= onDate && (!e.To.HasValue || onDate < e.To.Value))
                    .OrderByDescending(e => e.From)
                    .ThenByDescending(e => e.CreatedAtUtc)
                    .Select(e => e.TimesheetCodeDefinitionId)
                    .FirstOrDefault()
            })
            .Where(x => allowedCodeIds.Contains(x.CurrentCodeId))
            .Select(x => x.PersonId)
            .Distinct();

        // "Open tasks" block starting a new assignment for any mission.
        // Open assignment = To is null and started in the past.
        var openAssignmentsQuery =
            from a in db.MissionAssignments.AsNoTracking()
            join doc in db.CombatTaskDocuments.AsNoTracking() on a.SourceStartDocumentId equals doc.Id
            where doc.Status != DocumentStatus.Canceled
            where a.From <= onDate
            where a.To == null
            select a.PersonId;

        var q = eligibleByCodeQuery
            .Where(pid => !openAssignmentsQuery.Contains(pid))
            .OrderBy(pid => pid);

        return await q.ToListAsync(ct);
    }
}
