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
/// <b>Примітка:</b> ApplyCombatTaskFactsAsync працює через доменні методи агрегату,
/// щоб не дублювати правила (overlap/half-open/оновлення snapshot).
/// </para>
/// </summary>
public sealed class TimesheetAggregateRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetAggregateRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    // "Unit of Work" context for Load* + SaveChanges
    private AppDbContext? _db;

    //======================================================================
    // Apply facts from CombatTaskDetails
    //======================================================================

    /// <inheritdoc />
    public async Task ApplyCombatTaskFactsAsync(
        Guid documentId,
        Guid missionId,
        IReadOnlyCollection<CombatTaskDetails> details,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(details);
        if (documentId == Guid.Empty) throw new ArgumentException("documentId must be set.", nameof(documentId));
        if (missionId == Guid.Empty) throw new ArgumentException("missionId must be set.", nameof(missionId));
        if (string.IsNullOrWhiteSpace(author)) throw new ArgumentException("author is required.", nameof(author));

        if (details.Count == 0)
            return;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var documentRef = await db.CombatTaskDocuments
            .AsNoTracking()
            .Where(d => d.Id == documentId)
            .Select(d => d.OrderTitle)
            .SingleOrDefaultAsync(ct);

        documentRef = string.IsNullOrWhiteSpace(documentRef) ? null : documentRef.Trim();

        var perPerson = details
            .GroupBy(x => x.PersonId)
            .ToList();

        foreach (var g in perPerson)
        {
            var personId = g.Key;

            var startAt = g.Where(x => x.Kind == CombatTaskDetailsKind.Start)
                .Select(x => x.EffectiveAt)
                .DefaultIfEmpty(default)
                .Min();

            DateOnly? start = startAt == default ? null : startAt;

            var endAt = g.Where(x => x.Kind == CombatTaskDetailsKind.End)
                .Select(x => x.EffectiveAt)
                .DefaultIfEmpty(default)
                .Max();

            DateOnly? end = endAt == default ? null : endAt;

            if (start is null && end is null)
                continue;

            var refDate = start ?? end!.Value;

            // Епізод + TaskSpans (щоб виконати доменні правила на колекції)
            var episode = await db.TimeSheets
                .AsTracking()
                .Include(t => t.TaskSpans)
                .SingleOrDefaultAsync(t =>
                    t.PersonId == personId
                    && t.OpenedAt <= refDate
                    && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= refDate), ct)
                ?? throw new InvalidOperationException($"Active timesheet episode not found for person {personId} on {refDate}.");

            // Snapshot рядок (пріоритет: Start → End → будь-який)
            var snap = PickSnapshotRow(g);

            // 1) Start → upsert open span
            if (start.HasValue)
            {
                episode.UpsertTask(
                    documentId: documentId,
                    missionId: missionId,
                    from: start.Value,
                    toExclusive: null,
                    rnokpp: snap.Rnokpp ?? string.Empty,
                    fullName: snap.FullName ?? string.Empty,
                    rank: snap.Rank,
                    position: snap.Position,
                    weapon: snap.Weapon,
                    callsign: snap.Callsign,
                    openedByDocumentReference: documentRef,
                    author: author,
                    nowUtc: nowUtc);
            }

            // 2) End → close активний span на дату end (inclusive) => closeAtExclusive = end + 1
            if (end.HasValue)
            {
                var closeAtExclusive = end.Value.AddDays(1);

                episode.CloseTaskByDocument(
                    closingDocumentId: documentId,
                    missionId: missionId,
                    closeAtExclusive: closeAtExclusive,
                    rnokpp: snap.Rnokpp ?? string.Empty,
                    fullName: snap.FullName ?? string.Empty,
                    rank: snap.Rank,
                    position: snap.Position,
                    weapon: snap.Weapon,
                    callsign: snap.Callsign,
                    closedByDocumentReference: documentRef,
                    author: author,
                    nowUtc: nowUtc);
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task CancelCombatTaskFactsAsync(
        Guid documentId,
        Guid missionId,
        Guid reasonCodeId,
        string? reference,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("documentId must be set.", nameof(documentId));
        if (missionId == Guid.Empty) throw new ArgumentException("missionId must be set.", nameof(documentId));
        if (reasonCodeId == Guid.Empty) throw new ArgumentException("reasonCodeId must be set.", nameof(reasonCodeId));
        if (string.IsNullOrWhiteSpace(author)) throw new ArgumentException("author is required.", nameof(author));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var spans = await db.TimesheetTaskSpans
            .Where(s => s.MissionId == missionId)
            .Where(s => s.Status != DocumentStatus.Canceled)
            .Where(s => s.OpenedByCombatTaskDocumentId == documentId || s.ClosedByCombatTaskDocumentId == documentId)
            .ToListAsync(ct);

        if (spans.Count == 0)
            return;

        var trimmedRef = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();

        foreach (var span in spans)
        {
            span.Status = DocumentStatus.Canceled;
            span.ClosedByCodeId = reasonCodeId;
            span.ClosedReference = trimmedRef;
            span.UpdatedBy = author;
            span.UpdatedAtUtc = nowUtc;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
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
            .Include(t => t.TaskSpans)
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
            .Include(t => t.TaskSpans)
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

    //======================================================================
    // Helpers
    //======================================================================

    /// <summary>
    /// Вибирає рядок для snapshot-даних.
    /// Пріоритет: Start → End → будь-який.
    /// </summary>
    private static CombatTaskDetails PickSnapshotRow(IEnumerable<CombatTaskDetails> rows)
        => rows.FirstOrDefault(x => x.Kind == CombatTaskDetailsKind.Start)
           ?? rows.FirstOrDefault(x => x.Kind == CombatTaskDetailsKind.End)
           ?? rows.First();
}
