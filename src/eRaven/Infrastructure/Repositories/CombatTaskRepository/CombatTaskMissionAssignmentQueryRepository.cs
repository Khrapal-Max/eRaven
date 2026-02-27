//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskMissionAssignmentQueryRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// Query-репозиторій для планування/звітів по місіях (CombatTask).
///
/// <para>
/// Джерело правди по зайнятості на завданнях — CombatTask. Репозиторій читає матеріалізовані інтервали
/// призначень (<c>MissionAssignment</c>) і не залежить від табеля.
/// </para>
/// </summary>
public sealed class CombatTaskMissionAssignmentQueryRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ICombatTaskMissionAssignmentQueryRepository
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

        // Active on date (half-open): From <= D && (To is null || D < To)
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
    public async Task<IReadOnlyList<Guid>> GetPersonsWithOpenAssignmentsAsync(
        DateOnly onDate,
        CancellationToken ct = default)
    {
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Open assignment on date: From <= D && To is null
        var q =
            from a in db.MissionAssignments.AsNoTracking()
            join doc in db.CombatTaskDocuments.AsNoTracking() on a.SourceStartDocumentId equals doc.Id
            where doc.Status != DocumentStatus.Canceled
            where a.From <= onDate
            where a.To == null
            select a.PersonId;

        return await q.Distinct().OrderBy(x => x).ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetOccupiedPersonsInRangeAsync(
        DateOnly fromDate,
        DateOnly toExclusive,
        CancellationToken ct = default)
    {
        if (fromDate == default)
            throw new ArgumentException("from must be set.", nameof(fromDate));
        if (toExclusive == default)
            throw new ArgumentException("toExclusive must be set.", nameof(toExclusive));
        if (toExclusive <= fromDate)
            return [];

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Intersects range [from..toExclusive): a.From < toExclusive && (a.To is null || a.To > from)
        var q =
            from a in db.MissionAssignments.AsNoTracking()
            join doc in db.CombatTaskDocuments.AsNoTracking() on a.SourceStartDocumentId equals doc.Id
            where doc.Status != DocumentStatus.Canceled
            where a.From < toExclusive
            where a.To == null || a.To.Value > fromDate
            select a.PersonId;

        return await q.Distinct().OrderBy(x => x).ToListAsync(ct);
    }
}
