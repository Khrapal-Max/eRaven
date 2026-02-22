//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetAggregateRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// EF Core repository for <see cref="TimeSheetAggregate"/>.
///
/// <para>
/// Табель не "контролює" завдання: джерело правди — CombatTask.
/// Репозиторій виконує роль синхронізатора:
/// <list type="bullet">
/// <item><description>оновлює матеріалізовані призначення <see cref="MissionAssignment"/>;</description></item>
/// <item><description>пише/оновлює події табеля (30/100) як точки перемикання множини reference документів.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class TimesheetAggregateRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetAggregateRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    // "Unit of Work" context for Load* + SaveChanges
    private AppDbContext? _db;

    //======================================================================
    // Sync from CombatTaskDetails -> MissionAssignment -> Timesheet entries (30/100)
    //======================================================================

    /// <inheritdoc />
    public async Task ApplyCombatTaskFactsAsync(
        Guid documentId,
        string documentOrderTitle,
        Guid missionId,
        IReadOnlyCollection<CombatTaskDetails> details,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(details);
        if (documentId == Guid.Empty) throw new ArgumentException("documentId must be set.", nameof(documentId));
        if (string.IsNullOrWhiteSpace(documentOrderTitle)) throw new ArgumentException("documentOrderTitle is required.", nameof(documentOrderTitle));
        if (missionId == Guid.Empty) throw new ArgumentException("missionId must be set.", nameof(missionId));
        if (string.IsNullOrWhiteSpace(author)) throw new ArgumentException("author is required.", nameof(author));
        if (nowUtc == default) throw new ArgumentException("nowUtc must be set.", nameof(nowUtc));

        if (details.Count == 0)
            return;

        var documentRef = documentOrderTitle.Trim();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // -----------------------------------------------------------------
        // 1) Snapshot previous rows that were affected by this document
        //    - starts produced by this document
        //    - ends produced by this document
        // -----------------------------------------------------------------
        var previousStartedByDoc = await db.MissionAssignments
            .Where(x => x.MissionId == missionId && x.SourceStartDocumentId == documentId)
            .ToListAsync(ct);

        var previousEndedByDoc = await db.MissionAssignments
            .Where(x => x.MissionId == missionId && x.SourceEndDocumentId == documentId)
            .ToListAsync(ct);

        var extraDates = new HashSet<DateOnly>();
        AddBoundaryDates(extraDates, previousStartedByDoc);
        AddBoundaryDates(extraDates, previousEndedByDoc);

        var affectedPersons = new HashSet<Guid>(previousStartedByDoc.Select(x => x.PersonId));
        foreach (var p in previousEndedByDoc.Select(x => x.PersonId))
            affectedPersons.Add(p);

        // Who is present in this document right now
        var currentPersonIds = details
            .Select(x => x.PersonId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet();

        // -----------------------------------------------------------------
        // 2) Remove stale starts (document content is authoritative)
        // -----------------------------------------------------------------
        var staleStarts = previousStartedByDoc
            .Where(x => !currentPersonIds.Contains(x.PersonId))
            .ToList();

        if (staleStarts.Count > 0)
            db.MissionAssignments.RemoveRange(staleStarts);

        // -----------------------------------------------------------------
        // 3) Re-open stale ends (if document no longer contains End for that person)
        // -----------------------------------------------------------------
        foreach (var row in previousEndedByDoc.Where(x => !currentPersonIds.Contains(x.PersonId)))
        {
            // Clearing an end is not a no-op: guard it to avoid SQLite rows=0 => concurrency exception.
            if (row.To.HasValue || row.SourceEndDocumentId.HasValue || row.SourceEndDetailsId.HasValue)
            {
                extraDates.Add(row.To ?? default);
                row.To = null;
                row.SourceEndDocumentId = null;
                row.SourceEndDetailsId = null;
                row.UpdatedAtUtc = nowUtc;
                row.UpdatedBy = author;
            }
        }

        // -----------------------------------------------------------------
        // 4) Apply current details
        //    Notes:
        //    - Start+End can be in the same document.
        //    - End-only may close an interval started by other document.
        // -----------------------------------------------------------------
        foreach (var g in details.Where(x => x.PersonId != Guid.Empty).GroupBy(x => x.PersonId))
        {
            var personId = g.Key;
            affectedPersons.Add(personId);

            var startFact = g
                .Where(x => x.Kind == CombatTaskDetailsKind.Start)
                .OrderBy(x => x.EffectiveAt)
                .ThenBy(x => x.Id)
                .FirstOrDefault();

            var endFact = g
                .Where(x => x.Kind == CombatTaskDetailsKind.End)
                .OrderByDescending(x => x.EffectiveAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            var hasStart = startFact is not null && startFact.EffectiveAt != default;
            var hasEnd = endFact is not null && endFact.EffectiveAt != default;

            // If the document previously ended something for this person, but now there is no End in details — reopen.
            if (!hasEnd)
            {
                foreach (var row in previousEndedByDoc.Where(x => x.PersonId == personId).ToList())
                {
                    if (row.To.HasValue || row.SourceEndDocumentId.HasValue || row.SourceEndDetailsId.HasValue)
                    {
                        if (row.To.HasValue)
                            extraDates.Add(row.To.Value);

                        row.To = null;
                        row.SourceEndDocumentId = null;
                        row.SourceEndDetailsId = null;
                        row.UpdatedAtUtc = nowUtc;
                        row.UpdatedBy = author;
                    }
                }
            }

            if (hasStart)
            {
                extraDates.Add(startFact!.EffectiveAt);

                // Replace-all for "starts produced by this document" for this person (document content is authoritative).
                var oldStarts = previousStartedByDoc.Where(x => x.PersonId == personId).ToList();
                if (oldStarts.Count > 0)
                    db.MissionAssignments.RemoveRange(oldStarts);

                var toExclusive = hasEnd ? endFact!.EffectiveAt.AddDays(1) : (DateOnly?)null;
                if (toExclusive.HasValue)
                    extraDates.Add(toExclusive.Value);

                db.MissionAssignments.Add(new MissionAssignment
                {
                    Id = Guid.NewGuid(),
                    PersonId = personId,
                    MissionId = missionId,

                    From = startFact.EffectiveAt,
                    To = toExclusive,

                    SourceStartDocumentId = documentId,
                    SourceStartDetailsId = startFact.Id,

                    SourceEndDocumentId = hasEnd ? documentId : null,
                    SourceEndDetailsId = hasEnd ? endFact!.Id : null,

                    UpdatedAtUtc = nowUtc,
                    UpdatedBy = author
                });

                continue;
            }

            // No start in this document => remove stale starts produced by this doc.
            var staleForThisPerson = previousStartedByDoc.Where(x => x.PersonId == personId).ToList();
            if (staleForThisPerson.Count > 0)
                db.MissionAssignments.RemoveRange(staleForThisPerson);

            if (hasEnd)
            {
                // End-only: try close an active interval (prefer open-ended) for this mission+person.
                var endDate = endFact!.EffectiveAt;
                var toExclusive = endDate.AddDays(1);
                extraDates.Add(endDate);
                extraDates.Add(toExclusive);

                // Candidate: any interval active on endDate (not canceled start document)
                var candidate = await (
                        from a in db.MissionAssignments
                        join d in db.CombatTaskDocuments.AsNoTracking() on a.SourceStartDocumentId equals d.Id
                        where a.MissionId == missionId
                           && a.PersonId == personId
                           && d.Status != DocumentStatus.Canceled
                           && a.From <= endDate
                           && (!a.To.HasValue || endDate < a.To.Value)
                        orderby a.From descending
                        select a)
                    .FirstOrDefaultAsync(ct);

                if (candidate is not null)
                {
                    // SQLite: avoid no-op UPDATE
                    if (candidate.To != toExclusive
                        || candidate.SourceEndDocumentId != documentId
                        || candidate.SourceEndDetailsId != endFact.Id)
                    {
                        if (candidate.To.HasValue)
                            extraDates.Add(candidate.To.Value);

                        candidate.To = toExclusive;
                        candidate.SourceEndDocumentId = documentId;
                        candidate.SourceEndDetailsId = endFact.Id;
                        candidate.UpdatedAtUtc = nowUtc;
                        candidate.UpdatedBy = author;
                    }
                }
                else
                {
                    // Fallback: one-day interval owned by this document
                    db.MissionAssignments.Add(new MissionAssignment
                    {
                        Id = Guid.NewGuid(),
                        PersonId = personId,
                        MissionId = missionId,

                        From = endDate,
                        To = toExclusive,

                        SourceStartDocumentId = documentId,
                        SourceStartDetailsId = endFact.Id,
                        SourceEndDocumentId = documentId,
                        SourceEndDetailsId = endFact.Id,

                        UpdatedAtUtc = nowUtc,
                        UpdatedBy = author
                    });
                }
            }
        }

        await db.SaveChangesAsync(ct);

        // -----------------------------------------------------------------
        // 5) Sync timesheet entries (30/100) for affected persons
        // -----------------------------------------------------------------
        var codeIds = await LoadSystemCodeIdsAsync(db, ct);

        foreach (var personId in affectedPersons)
        {
            await RebuildTimesheetForPersonAsync(
                db: db,
                personId: personId,
                missionId: missionId,
                taskCodeId: codeIds.TaskCodeId,
                readyCodeId: codeIds.ReadyCodeId,
                extraDates: extraDates,
                author: author,
                nowUtc: nowUtc,
                ct: ct);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task CancelCombatTaskFactsAsync(
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

        // "Cancel" means: this document must have no effect on timesheet.
        // 1) Remove intervals started by this document
        // 2) Re-open intervals ended by this document
        var started = await db.MissionAssignments
            .Where(x => x.MissionId == missionId && x.SourceStartDocumentId == documentId)
            .ToListAsync(ct);

        var ended = await db.MissionAssignments
            .Where(x => x.MissionId == missionId && x.SourceEndDocumentId == documentId)
            .ToListAsync(ct);

        if (started.Count == 0 && ended.Count == 0)
            return;

        var affectedPersons = new HashSet<Guid>(started.Select(x => x.PersonId));
        foreach (var p in ended.Select(x => x.PersonId))
            affectedPersons.Add(p);

        var extraDates = new HashSet<DateOnly>();
        AddBoundaryDates(extraDates, started);
        AddBoundaryDates(extraDates, ended);

        if (started.Count > 0)
            db.MissionAssignments.RemoveRange(started);

        foreach (var row in ended)
        {
            // SQLite: avoid no-op update
            if (row.To.HasValue || row.SourceEndDocumentId.HasValue || row.SourceEndDetailsId.HasValue)
            {
                if (row.To.HasValue)
                    extraDates.Add(row.To.Value);

                row.To = null;
                row.SourceEndDocumentId = null;
                row.SourceEndDetailsId = null;
                row.UpdatedAtUtc = nowUtc;
                row.UpdatedBy = author;
            }
        }

        await db.SaveChangesAsync(ct);

        var codeIds = await LoadSystemCodeIdsAsync(db, ct);

        foreach (var personId in affectedPersons)
        {
            await RebuildTimesheetForPersonAsync(
                db: db,
                personId: personId,
                missionId: missionId,
                taskCodeId: codeIds.TaskCodeId,
                readyCodeId: codeIds.ReadyCodeId,
                extraDates: extraDates,
                author: author,
                nowUtc: nowUtc,
                ct: ct);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private sealed record SystemCodeIds(Guid ReadyCodeId, Guid TaskCodeId);

    private static async Task<SystemCodeIds> LoadSystemCodeIdsAsync(AppDbContext db, CancellationToken ct)
    {
        var readyCodeId = await db.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.Code == TimesheetSystemCodes.ReadyToCombatTask)
            .Select(x => x.Id)
            .SingleAsync(ct);

        var taskCodeId = await db.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.Code == TimesheetSystemCodes.DoesTheCombatTask)
            .Select(x => x.Id)
            .SingleAsync(ct);

        return new SystemCodeIds(readyCodeId, taskCodeId);
    }

    private static void AddBoundaryDates(HashSet<DateOnly> set, IEnumerable<MissionAssignment> rows)
    {
        foreach (var r in rows)
        {
            if (r.From != default)
                set.Add(r.From);

            if (r.To.HasValue)
                set.Add(r.To.Value);
        }
    }

    private static async Task RebuildTimesheetForPersonAsync(
        AppDbContext db,
        Guid personId,
        Guid missionId,
        Guid taskCodeId,
        Guid readyCodeId,
        HashSet<DateOnly> extraDates,
        string author,
        DateTime nowUtc,
        CancellationToken ct)
    {
        // Get all active assignments for mission+person (across all docs)
        var assignments = await (
                from a in db.MissionAssignments.AsNoTracking()
                join d in db.CombatTaskDocuments.AsNoTracking() on a.SourceStartDocumentId equals d.Id
                where a.MissionId == missionId
                   && a.PersonId == personId
                   && d.Status != DocumentStatus.Canceled
                select a)
            .ToListAsync(ct);

        var dates = new SortedSet<DateOnly>(extraDates);
        foreach (var a in assignments)
        {
            dates.Add(a.From);
            if (a.To.HasValue)
                dates.Add(a.To.Value);
        }

        if (dates.Count == 0)
            return;

        var firstDate = dates.Min;

        var episode = await db.TimeSheets
            .AsTracking()
            .Include(t => t.Entries)
            .SingleOrDefaultAsync(t =>
                t.PersonId == personId
                && t.OpenedAt <= firstDate
                && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= firstDate), ct)
            ?? throw new InvalidOperationException($"Timesheet episode not found for person {personId} on {firstDate}.");

        // ---------------------------------------------------------------
        // Controlled cleanup
        // ---------------------------------------------------------------

        // If there are no assignments now => ensure only baseline controlled entry remains.
        if (assignments.Count == 0)
        {
            var redundant = episode.Entries
                .Where(e => !e.IsDeleted
                    && (e.TimesheetCodeDefinitionId == taskCodeId || e.TimesheetCodeDefinitionId == readyCodeId)
                    && e.From != episode.OpenedAt)
                .OrderByDescending(e => e.From)
                .ToList();

            foreach (var e in redundant)
                episode.RemoveTimesheetEntry(e.Id);

            return;
        }

        // Remove all controlled entries from first affected date forward (except baseline at OpenedAt)
        var controlled = episode.Entries
            .Where(e => !e.IsDeleted
                && (e.TimesheetCodeDefinitionId == taskCodeId || e.TimesheetCodeDefinitionId == readyCodeId)
                && e.From >= firstDate
                && e.From != episode.OpenedAt)
            .OrderByDescending(e => e.From)
            .ToList();

        foreach (var e in controlled)
            episode.RemoveTimesheetEntry(e.Id);

        // ---------------------------------------------------------------
        // Build change map based on active set of DocumentReference
        // ---------------------------------------------------------------
        var changes = new SortedDictionary<DateOnly, (List<string> Add, List<string> Remove)>();
        foreach (var d in dates)
            changes[d] = (new List<string>(), new List<string>());

        var active = new SortedSet<string>(StringComparer.Ordinal);
        active.RemoveWhere(s => string.IsNullOrWhiteSpace(s));

        // Start with the currently effective state at firstDate (after controlled cleanup).
        var initial = episode.Entries
            .Where(e => !e.IsDeleted && e.From <= firstDate)
            .OrderByDescending(e => e.From)
            .FirstOrDefault();

        string lastRef = (initial?.Reference ?? string.Empty).Trim();
        var lastCode = initial?.TimesheetCodeDefinitionId ?? Guid.Empty;

        foreach (var kv in changes)
        {
            var date = kv.Key;
            var (add, remove) = kv.Value;

            // removals first (end at 'date' means not active on 'date')
            foreach (var r in remove)
            {
                var s = (r ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(s))
                    active.Remove(s);
            }

            // adds (start at 'date' means active on 'date')
            foreach (var a in add)
            {
                var s = (a ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(s))
                    active.Add(s);
            }

            var hasTask = active.Count > 0;
            var codeId = hasTask ? taskCodeId : readyCodeId;
            var refs = hasTask ? string.Join(", ", active) : string.Empty;

            if (codeId == lastCode && string.Equals(refs, lastRef, StringComparison.Ordinal))
                continue;

            UpsertTimesheetEntryAtDate(episode, codeId, date, refs, author, nowUtc);

            lastCode = codeId;
            lastRef = refs;
        }
    }

    private static void AddChange(
        SortedDictionary<DateOnly, (List<string> Add, List<string> Remove)> map,
        DateOnly date,
        string? add,
        string? remove)
    {
        if (!map.TryGetValue(date, out var entry))
        {
            entry = (new List<string>(), new List<string>());
            map[date] = entry;
        }

        if (!string.IsNullOrWhiteSpace(add))
            entry.Add.Add(add);

        if (!string.IsNullOrWhiteSpace(remove))
            entry.Remove.Add(remove);
    }

    private static void UpsertTimesheetEntryAtDate(
        TimeSheetAggregate episode,
        Guid nextCodeId,
        DateOnly effectiveAt,
        string? references,
        string author,
        DateTime nowUtc)
    {
        // find existing by date (avoid LINQ on indexable collections warning is not critical here)
        TimesheetEntry? existing = null;
        var entries = episode.Entries
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.From)
            .ToList();

        for (var i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (!e.IsDeleted && e.From == effectiveAt)
            {
                existing = e;
                break;
            }
        }

        if (existing is null)
        {
            episode.AddTimesheetEntry(
                nextCodeId: nextCodeId,
                effectiveAt: effectiveAt,
                reference: references,
                author: author,
                nowUtc: nowUtc);
            return;
        }

        var nextRef = (references ?? string.Empty).Trim();

        // SQLite: avoid no-op UPDATE (rows=0 -> concurrency exception).
        if (existing.TimesheetCodeDefinitionId == nextCodeId
            && string.Equals((existing.Reference ?? string.Empty).Trim(), nextRef, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(existing.Note))
        {
            return;
        }

        episode.CorrectionTimesheetEntry(
            entyId: existing.Id,
            nextCodeId: nextCodeId,
            fromEffectiveAt: effectiveAt,
            reference: nextRef,
            author: author,
            nowUtc: nowUtc);
    }

    //======================================================================
    // Load + Save (unit of work)
    //======================================================================

    /// <inheritdoc />
    public async Task<TimeSheetAggregate> LoadActiveAsync(Guid personId, CancellationToken ct = default)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("personId must be set.", nameof(personId));

        _db ??= await _dbFactory.CreateDbContextAsync(ct);

        var ep = await _db.TimeSheets
            .Include(t => t.Entries)
            .SingleOrDefaultAsync(t => t.PersonId == personId && t.ClosedAt == null, ct);

        return ep ?? throw new InvalidOperationException("Active timesheet episode not found.");
    }

    /// <inheritdoc />
    public async Task<TimeSheetAggregate> LoadOnDateForUpdateAsync(Guid personId, DateOnly onDate, CancellationToken ct = default)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("personId must be set.", nameof(personId));
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));

        _db ??= await _dbFactory.CreateDbContextAsync(ct);

        var ep = await _db.TimeSheets
            .Include(t => t.Entries)
            .SingleOrDefaultAsync(t =>
                t.PersonId == personId
                && t.OpenedAt <= onDate
                && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= onDate), ct);

        return ep ?? throw new InvalidOperationException("Timesheet episode not found for the specified date.");
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        if (_db is null)
            return;

        await _db.SaveChangesAsync(ct);
    }
}
