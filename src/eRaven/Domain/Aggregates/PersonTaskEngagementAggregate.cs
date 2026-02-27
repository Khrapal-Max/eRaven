//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonTaskEngagementAggregate
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Domain.Aggregates;

/// <summary>
/// Агрегат зайнятості особи в домені бойових завдань.
///
/// <para>
/// Призначення агрегата:
/// <list type="bullet">
/// <item><description>гарантувати інваріант "особа має максимум одне активне (open-ended) завдання";</description></item>
/// <item><description>атомарно застосовувати факти Start/End (у т.ч. End в іншому документі);</description></item>
/// <item><description>підтримувати компенсацію документа (Cancel/Void) без фізичного видалення факту документа.</description></item>
/// </list>
/// </para>
///
/// <para>
/// Важливо: агрегат не керує зовнішнім "потоком подій" (оркестрацією) —
/// він лише змінює свій стан (інтервали) і повертає мінімальний опис змін.
/// </para>
/// </summary>
public sealed class PersonTaskEngagementAggregate
{
    private readonly List<MissionAssignment> _assignments;

    /// <summary>Ідентифікатор особи.</summary>
    public Guid PersonId { get; }

    /// <summary>
    /// Поточні інтервали зайнятості особи (half-open: <c>[From..To)</c>).
    /// </summary>
    public IReadOnlyList<MissionAssignment> Assignments => _assignments;

    /// <summary>
    /// Створює агрегат для особи з існуючими інтервалами (з БД).
    /// </summary>
    public PersonTaskEngagementAggregate(Guid personId, IEnumerable<MissionAssignment>? existing)
    {
        if (personId == Guid.Empty) throw new ArgumentException("personId must be set.", nameof(personId));

        PersonId = personId;
        _assignments = existing is null ? [] : [.. existing.OrderBy(x => x.From)];

        // defensive: ensure state consistency
        EnsureStateIsValid();
    }

    //======================================================================
    // Commands
    //======================================================================

    /// <summary>
    /// Застосовує факт початку завдання (Start).
    /// </summary>
    /// <remarks>
    /// Інваріанти:
    /// <list type="number">
    /// <item><description>на особу може існувати не більше одного open-ended інтервалу (To == null);</description></item>
    /// <item><description>новий інтервал не повинен перетинатися з існуючими (day-level, без часу), окрім "handover day";</description></item>
    /// <item><description>факт Start (DetailsId) має бути ідемпотентним (не застосовується двічі).</description></item>
    /// </list>
    /// </remarks>
    public IReadOnlyList<EngagementChange> ApplyStart(
        Guid missionId,
        DateOnly from,
        Guid startDocumentId,
        Guid startDetailsId,
        DateOnly? endInclusive,
        Guid? endDocumentId,
        Guid? endDetailsId,
        string author,
        DateTime nowUtc)
    {
        if (missionId == Guid.Empty) throw new ArgumentException("missionId must be set.", nameof(missionId));
        if (from == default) throw new ArgumentException("from must be set.", nameof(from));
        if (startDocumentId == Guid.Empty) throw new ArgumentException("startDocumentId must be set.", nameof(startDocumentId));
        if (startDetailsId == Guid.Empty) throw new ArgumentException("startDetailsId must be set.", nameof(startDetailsId));
        if (string.IsNullOrWhiteSpace(author)) throw new ArgumentException("author is required.", nameof(author));
        if (nowUtc == default) throw new ArgumentException("nowUtc must be set.", nameof(nowUtc));

        // Idempotency: Start DetailsId is globally unique and must not be applied twice.
        if (_assignments.Any(a => a.SourceStartDetailsId == startDetailsId))
            return [];

        // Normalize end identifiers when end date is supplied (closed interval inside the same document by default).
        DateOnly? toExclusive = null;
        if (endInclusive.HasValue)
        {
            if (endInclusive.Value == default) throw new ArgumentException("endInclusive must be a valid date.", nameof(endInclusive));
            if (endInclusive.Value < from) throw new InvalidOperationException("End date cannot be earlier than start date.");

            if (endDocumentId.HasValue && endDocumentId.Value == Guid.Empty)
                throw new ArgumentException("endDocumentId must be a valid id.", nameof(endDocumentId));
            if (endDetailsId.HasValue && endDetailsId.Value == Guid.Empty)
                throw new ArgumentException("endDetailsId must be a valid id.", nameof(endDetailsId));

            endDocumentId ??= startDocumentId;
            endDetailsId ??= startDetailsId;

            toExclusive = endInclusive.Value.AddDays(1);
        }

        EnsureCanStart(from, toExclusive);

        var aNew = new MissionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = PersonId,
            MissionId = missionId,
            From = from,
            To = toExclusive,
            SourceStartDocumentId = startDocumentId,
            SourceStartDetailsId = startDetailsId,
            SourceEndDocumentId = endDocumentId,
            SourceEndDetailsId = endDetailsId,
            UpdatedBy = author,
            UpdatedAtUtc = nowUtc
        };

        _assignments.Add(aNew);
        _assignments.Sort((x, y) => x.From.CompareTo(y.From));

        var changes = new List<EngagementChange>
        {
            EngagementChange.Started(PersonId, missionId, from, startDocumentId, startDetailsId)
        };

        if (toExclusive.HasValue)
        {
            changes.Add(EngagementChange.Ended(PersonId, missionId, toExclusive.Value, endDocumentId!.Value, endDetailsId!.Value));
        }

        EnsureStateIsValid();
        return changes;
    }

    /// <summary>
    /// Застосовує факт завершення завдання (End) у "документі закриття".
    /// </summary>
    /// <remarks>
    /// Якщо активний інтервал відсутній, метод створює одноденний інтервал <c>[end..end+1)</c>
    /// (fallback для неповних даних).
    /// </remarks>
    public IReadOnlyList<EngagementChange> ApplyEnd(
        Guid missionId,
        DateOnly endInclusive,
        Guid endDocumentId,
        Guid endDetailsId,
        string author,
        DateTime nowUtc)
    {
        if (missionId == Guid.Empty) throw new ArgumentException("missionId must be set.", nameof(missionId));
        if (endInclusive == default) throw new ArgumentException("endInclusive must be set.", nameof(endInclusive));
        if (endDocumentId == Guid.Empty) throw new ArgumentException("endDocumentId must be set.", nameof(endDocumentId));
        if (endDetailsId == Guid.Empty) throw new ArgumentException("endDetailsId must be set.", nameof(endDetailsId));
        if (string.IsNullOrWhiteSpace(author)) throw new ArgumentException("author is required.", nameof(author));
        if (nowUtc == default) throw new ArgumentException("nowUtc must be set.", nameof(nowUtc));

        // Idempotency: End DetailsId is globally unique and must not be applied twice.
        if (_assignments.Any(a => a.SourceEndDetailsId == endDetailsId))
            return [];

        var toExclusive = endInclusive.AddDays(1);

        // Preferred: close current open interval for this mission.
        var open = _assignments
            .Where(a => a.MissionId == missionId && !a.To.HasValue)
            .OrderByDescending(a => a.From)
            .FirstOrDefault();

        if (open is not null)
        {
            if (open.From > endInclusive)
                throw new InvalidOperationException("End date cannot be earlier than start date.");

            open.To = toExclusive;
            open.SourceEndDocumentId = endDocumentId;
            open.SourceEndDetailsId = endDetailsId;
            open.UpdatedBy = author;
            open.UpdatedAtUtc = nowUtc;

            EnsureStateIsValid();
            return
            [
                EngagementChange.Ended(PersonId, missionId, toExclusive, endDocumentId, endDetailsId)
            ];
        }

        // Fallback: one-day engagement for the given end day.
        EnsureCanStart(endInclusive, toExclusive);

        var aNew = new MissionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = PersonId,
            MissionId = missionId,
            From = endInclusive,
            To = toExclusive,
            SourceStartDocumentId = endDocumentId,
            SourceStartDetailsId = endDetailsId,
            SourceEndDocumentId = endDocumentId,
            SourceEndDetailsId = endDetailsId,
            UpdatedBy = author,
            UpdatedAtUtc = nowUtc
        };

        _assignments.Add(aNew);
        _assignments.Sort((x, y) => x.From.CompareTo(y.From));

        EnsureStateIsValid();
        return
        [
            EngagementChange.Started(PersonId, missionId, endInclusive, endDocumentId, endDetailsId),
            EngagementChange.Ended(PersonId, missionId, toExclusive, endDocumentId, endDetailsId)
        ];
    }

    /// <summary>
    /// Компенсація (Cancel/Void) документа.
    /// </summary>
    /// <remarks>
    /// Правило:
    /// <list type="bullet">
    /// <item><description>інтервали, які документ стартував — видаляємо;</description></item>
    /// <item><description>інтервали, які документ закрив — "розкриваємо" (To = null).</description></item>
    /// </list>
    /// </remarks>
    public IReadOnlyList<EngagementChange> CompensateDocument(
        Guid documentId,
        Guid? missionId,
        string author,
        DateTime nowUtc)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("documentId must be set.", nameof(documentId));
        if (missionId == Guid.Empty) throw new ArgumentException("missionId must be a valid id.", nameof(missionId));
        if (string.IsNullOrWhiteSpace(author)) throw new ArgumentException("author is required.", nameof(author));
        if (nowUtc == default) throw new ArgumentException("nowUtc must be set.", nameof(nowUtc));

        var changes = new List<EngagementChange>();

        // 1) Remove intervals started by this document.
        var started = _assignments
            .Where(a => a.SourceStartDocumentId == documentId)
            .Where(a => !missionId.HasValue || a.MissionId == missionId.Value)
            .ToList();

        foreach (var a in started)
        {
            // For timesheet it is important to know that the person becomes free at a.From.
            changes.Add(EngagementChange.Removed(PersonId, a.MissionId, a.From, documentId, a.SourceStartDetailsId));
            _assignments.Remove(a);
        }

        // 2) Reopen intervals ended by this document.
        var ended = _assignments
            .Where(a => a.SourceEndDocumentId == documentId)
            .Where(a => !missionId.HasValue || a.MissionId == missionId.Value)
            .ToList();

        foreach (var a in ended)
        {
            a.To = null;
            a.SourceEndDocumentId = null;
            a.SourceEndDetailsId = null;
            a.UpdatedBy = author;
            a.UpdatedAtUtc = nowUtc;

            changes.Add(EngagementChange.Reopened(PersonId, a.MissionId, a.From, documentId, a.SourceStartDetailsId));
        }

        EnsureStateIsValid();
        return changes;
    }

    //======================================================================
    // Invariants / Guards
    //======================================================================

    private void EnsureStateIsValid()
    {
        EnsureNoMultipleOpenIntervals();
        EnsureNoOverlapsAndOrdering();
        EnsureIntervalEndConsistency();
    }

    private void EnsureNoMultipleOpenIntervals()
    {
        var openCount = _assignments.Count(a => !a.To.HasValue);
        if (openCount > 1)
            throw new InvalidOperationException("Person has multiple active tasks (open-ended intervals). This is not allowed.");
    }

    private void EnsureIntervalEndConsistency()
    {
        foreach (var a in _assignments)
        {
            if (a.PersonId != PersonId)
                throw new InvalidOperationException("Invariant violated: assignment belongs to a different person.");

            if (a.MissionId == Guid.Empty)
                throw new InvalidOperationException("Invariant violated: MissionId must be set.");

            if (a.From == default)
                throw new InvalidOperationException("Invariant violated: From date must be set.");

            if (a.SourceStartDocumentId == Guid.Empty || a.SourceStartDetailsId == Guid.Empty)
                throw new InvalidOperationException("Invariant violated: Start source ids must be set.");

            if (a.To.HasValue)
            {
                if (a.To.Value <= a.From)
                    throw new InvalidOperationException("Invariant violated: To must be greater than From.");

                if (!a.SourceEndDocumentId.HasValue || !a.SourceEndDetailsId.HasValue)
                    throw new InvalidOperationException("Invariant violated: Closed interval must have End source ids.");
            }
            else
            {
                if (a.SourceEndDocumentId.HasValue || a.SourceEndDetailsId.HasValue)
                    throw new InvalidOperationException("Invariant violated: Open interval must not have End source ids.");
            }
        }
    }

    private void EnsureNoOverlapsAndOrdering()
    {
        if (_assignments.Count <= 1)
            return;

        var ordered = _assignments.OrderBy(x => x.From).ToList();

        for (var i = 0; i < ordered.Count - 1; i++)
        {
            var current = ordered[i];
            var next = ordered[i + 1];

            // Open interval must be the last one.
            if (!current.To.HasValue)
                throw new InvalidOperationException("Invariant violated: open-ended interval must be the last one.");

            // Overlap detection (half-open): current.From < next.To && next.From < current.To
            // Since we iterate in From order, it's enough to ensure next.From >= current.To,
            // except the allowed "handover day" where next.From == current.To - 1 day.
            if (next.From < current.To.Value)
            {
                var isHandoverDay = next.From == current.To.Value.AddDays(-1);
                if (!isHandoverDay)
                    throw new InvalidOperationException("Invariant violated: task intervals overlap.");
            }
        }
    }

    private void EnsureCanStart(DateOnly from, DateOnly? toExclusive)
    {
        // 1) Cannot start if there is an open-ended task.
        if (_assignments.Any(a => !a.To.HasValue))
            throw new InvalidOperationException("Person already has an active task.");

        // 2) Prevent overlaps with existing closed intervals.
        var newTo = toExclusive;
        foreach (var a in _assignments)
        {
            var aTo = a.To;
            if (!aTo.HasValue)
                continue; // already covered above

            // overlap if a.From < newTo && from < aTo
            // If newTo is null (open), treat it as infinity; it overlaps with any interval that ends after from.
            var overlaps = newTo.HasValue
                ? (a.From < newTo.Value && from < aTo.Value)
                : (from < aTo.Value);

            // ✅ Allow "handover day" overlap: start on previous EndInclusive day.
            // Existing interval is [..aTo), so its last active day is (aTo - 1 day).
            var isHandoverDay = from == aTo.Value.AddDays(-1);

            if (overlaps && !isHandoverDay)
                throw new InvalidOperationException("Task interval overlaps with an existing interval.");
        }
    }
}

/// <summary>
/// Опис мінімальної зміни стану зайнятості (для оркестрації та формування фактів табеля).
/// </summary>
public sealed record EngagementChange(
    EngagementChangeKind Kind,
    Guid PersonId,
    Guid MissionId,
    DateOnly EffectiveAt,
    Guid SourceDocumentId,
    Guid SourceDetailsId)
{
    public static EngagementChange Started(Guid personId, Guid missionId, DateOnly effectiveAt, Guid docId, Guid detailsId)
        => new(EngagementChangeKind.Started, personId, missionId, effectiveAt, docId, detailsId);

    /// <summary>
    /// <paramref name="effectiveAt"/> here is the <b>ToExclusive</b> date (the first free day).
    /// </summary>
    public static EngagementChange Ended(Guid personId, Guid missionId, DateOnly effectiveAt, Guid docId, Guid detailsId)
        => new(EngagementChangeKind.Ended, personId, missionId, effectiveAt, docId, detailsId);

    public static EngagementChange Reopened(Guid personId, Guid missionId, DateOnly effectiveAt, Guid docId, Guid detailsId)
        => new(EngagementChangeKind.Reopened, personId, missionId, effectiveAt, docId, detailsId);

    public static EngagementChange Removed(Guid personId, Guid missionId, DateOnly effectiveAt, Guid docId, Guid detailsId)
        => new(EngagementChangeKind.Removed, personId, missionId, effectiveAt, docId, detailsId);
}
