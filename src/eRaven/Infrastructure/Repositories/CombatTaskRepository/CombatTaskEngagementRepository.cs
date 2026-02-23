//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskEngagementRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

public sealed class CombatTaskEngagementRepository(IDbContextFactory<AppDbContext> dbFactory) : ICombatTaskEngagementRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> ApplyMissionFactsAsync(
        Guid documentId,
        Guid missionId,
        IReadOnlyCollection<CombatTaskDetails> details,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("documentId must be set.", nameof(documentId));
        if (missionId == Guid.Empty) throw new ArgumentException("missionId must be set.", nameof(missionId));
        ArgumentNullException.ThrowIfNull(details);
        if (string.IsNullOrWhiteSpace(author)) throw new ArgumentException("author is required.", nameof(author));
        if (nowUtc == default) throw new ArgumentException("nowUtc must be set.", nameof(nowUtc));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Guard: canceled document cannot apply new facts
        var status = await db.CombatTaskDocuments
            .AsNoTracking()
            .Where(x => x.Id == documentId)
            .Select(x => x.Status)
            .SingleAsync(ct);

        if (status == DocumentStatus.Canceled)
            throw new InvalidOperationException("Документ скасовано. Застосування фактів заборонено.");

        // Previous intervals started/ended by this document for this mission.
        var prevForMission = await db.MissionAssignments
            .AsNoTracking()
            .Where(x => x.MissionId == missionId)
            .Where(x => x.SourceStartDocumentId == documentId || x.SourceEndDocumentId == documentId)
            .ToListAsync(ct);

        var affectedPersons = new HashSet<Guid>(prevForMission.Select(x => x.PersonId));

        foreach (var pid in details.Select(x => x.PersonId).Where(x => x != Guid.Empty))
            affectedPersons.Add(pid);

        var keepPersons = details.Select(x => x.PersonId).Where(x => x != Guid.Empty).Distinct().ToHashSet();

        // Per-person apply (tracked) to avoid cross-person conflicts.
        foreach (var personId in affectedPersons)
        {
            // Load all assignments for person (tracked) to enforce "one active" invariant.
            var personRows = await db.MissionAssignments
                .Where(x => x.PersonId == personId)
                .ToListAsync(ct);

            var agg = new PersonTaskEngagementAggregate(personId, personRows);

            // Replace-all semantics for this document+mission: compensate missing persons.
            if (!keepPersons.Contains(personId))
            {
                var changes = agg.CompensateDocument(documentId, missionId, author, nowUtc);
                ApplyAggregateStateToDb(personRows, agg, db);
                continue;
            }

            // Facts for this person for this mission.
            var pDetails = details.Where(x => x.PersonId == personId).ToList();

            var start = pDetails
                .Where(x => x.Kind == CombatTaskDetailsKind.Start)
                .OrderBy(x => x.EffectiveAt)
                .FirstOrDefault();

            var end = pDetails
                .Where(x => x.Kind == CombatTaskDetailsKind.End)
                .OrderByDescending(x => x.EffectiveAt)
                .FirstOrDefault();

            if (start is not null)
            {
                // Compensate any previous start by this document for this mission (edit/update scenario)
                agg.CompensateDocument(documentId, missionId, author, nowUtc);

                agg.ApplyStart(
                    missionId: missionId,
                    from: start.EffectiveAt,
                    startDocumentId: documentId,
                    startDetailsId: start.Id,
                    endInclusive: end?.EffectiveAt,
                    endDocumentId: end is null ? null : documentId,
                    endDetailsId: end?.Id,
                    author: author,
                    nowUtc: nowUtc);
            }
            else if (end is not null)
            {
                // End-only: close open interval started by other document
                agg.ApplyEnd(
                    missionId: missionId,
                    endInclusive: end.EffectiveAt,
                    endDocumentId: documentId,
                    endDetailsId: end.Id,
                    author: author,
                    nowUtc: nowUtc);
            }

            ApplyAggregateStateToDb(personRows, agg, db);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return [.. affectedPersons.OrderBy(x => x)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> CancelMissionFactsAsync(
        Guid documentId,
        Guid missionId,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("documentId must be set.", nameof(documentId));
        if (missionId == Guid.Empty) throw new ArgumentException("missionId must be set.", nameof(missionId));
        if (string.IsNullOrWhiteSpace(author)) throw new ArgumentException("author is required.", nameof(author));
        if (nowUtc == default) throw new ArgumentException("nowUtc must be set.", nameof(nowUtc));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Persons affected by this document+mission
        var affectedPersons = await db.MissionAssignments
            .AsNoTracking()
            .Where(x => x.MissionId == missionId)
            .Where(x => x.SourceStartDocumentId == documentId || x.SourceEndDocumentId == documentId)
            .Select(x => x.PersonId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var personId in affectedPersons)
        {
            var personRows = await db.MissionAssignments
                .Where(x => x.PersonId == personId)
                .ToListAsync(ct);

            var agg = new PersonTaskEngagementAggregate(personId, personRows);
            agg.CompensateDocument(documentId, missionId, author, nowUtc);
            ApplyAggregateStateToDb(personRows, agg, db);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return [.. affectedPersons.OrderBy(x => x)];
    }

    private static void ApplyAggregateStateToDb(
        List<MissionAssignment> trackedRows,
        PersonTaskEngagementAggregate agg,
        AppDbContext db)
    {
        // trackedRows are EF tracked, agg.Assignments includes same instances + newly created instances.
        // Add new
        var trackedIds = trackedRows.Select(x => x.Id).ToHashSet();
        foreach (var a in agg.Assignments)
        {
            if (!trackedIds.Contains(a.Id))
                db.MissionAssignments.Add(a);
        }

        // Remove deleted: those tracked but not present anymore.
        var aggIds = agg.Assignments.Select(x => x.Id).ToHashSet();
        foreach (var old in trackedRows)
        {
            if (!aggIds.Contains(old.Id))
                db.MissionAssignments.Remove(old);
        }
    }
}
