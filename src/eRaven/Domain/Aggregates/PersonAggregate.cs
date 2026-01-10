//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonAggregate
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Events;
using eRaven.Domain.ValueObjects;

namespace eRaven.Domain.Aggregates;

public sealed class PersonAggregate
{
    // Envelope для ре-хайдрата со stream version
    public readonly record struct StoredEvent(long Version, IDomainEvent Event);

    private readonly List<IDomainEvent> _changes = [];
    private readonly HashSet<Guid> _voidedEventIds = [];

    public Guid Id { get; private set; }
    public long Version { get; private set; } // stream version (последний persisted version)

    public PersonLifecycle Lifecycle { get; private set; } = PersonLifecycle.Candidate;

    public PersonalInfo? Personal { get; private set; }

    public string? Rank { get; private set; }

    // PlannedPositionUnitId - резерв для кандидата
    public Guid? PlannedPositionUnitId { get; private set; }     // резерв для кандидата
    public string? PlannedPosition { get; private set; }

    // PositionUnitId - основна посада
    public Guid? PositionUnitId { get; private set; }            // основна посада (occupied)
    public string? Position { get; private set; }

    // TemporaryPositionUnitId - тимчасова посада
    public Guid? TemporaryPositionUnitId { get; private set; }   // тимчасова посада (temporarily occupied)
    public string? TemporaryPosition { get; private set; }

    public string? BZVP { get; private set; }
    public string? Weapon { get; private set; }
    public string? Callsign { get; private set; }

    public EnrollmentKind? EnrollmentKind { get; private set; }
    public string? EnrollmentReference { get; private set; }

    public DateOnly? EnrolledAt { get; private set; }
    public DateOnly? ExcludedAt { get; private set; }

    // ============================
    // Uncommitted changes
    // ============================

    public IReadOnlyList<IDomainEvent> GetUncommittedChanges() => _changes;
    public void ClearUncommittedChanges() => _changes.Clear();

    internal void MarkChangesAsCommitted(long newVersion)
    {
        Version = newVersion;
        _changes.Clear();
    }

    // Канонический ES: применить к состоянию + отследить как uncommitted
    private void Raise(IDomainEvent e)
    {
        Apply(e);
        _changes.Add(e);
    }

    // ============================
    // Commands
    // ============================

    public static PersonAggregate CreateCandidate(
        Guid id,
        PersonalInfo personal,
        string? plannedPosition,
        Guid? plannedPositionUnitId,
        string author,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("AggregateId is required.", nameof(id));

        ArgumentNullException.ThrowIfNull(personal);

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        var a = new PersonAggregate();

        a.Raise(new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: personal,
            PlannedPosition: Normalize(plannedPosition),
            PlannedPositionUnitId: plannedPositionUnitId, // NEW
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));

        return a;
    }

    public void UpdatePersonalInfo(PersonalInfo personal, string? plannedPosition, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        ArgumentNullException.ThrowIfNull(personal);

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        Raise(new PersonPersonalInfoUpdated(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            Personal: personal,
            PlannedPosition: Normalize(plannedPosition),
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));
    }

    public void ChangeRank(DateOnly effectiveDate, string rank, string? note, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        if (string.IsNullOrWhiteSpace(rank))
            throw new ArgumentException("Rank is required.", nameof(rank));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        Raise(new PersonRankChanged(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            EffectiveDate: effectiveDate,
            Rank: rank.Trim(),
            Note: Normalize(note),
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));
    }

    public void ChangePosition(DateOnly effectiveDate, Guid positionUnitId, string position, string? note, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        if (positionUnitId == Guid.Empty)
            throw new ArgumentException("PositionUnitId is required.", nameof(positionUnitId));

        if (string.IsNullOrWhiteSpace(position))
            throw new ArgumentException("Position is required.", nameof(position));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        Raise(new PersonPositionChanged(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            EffectiveDate: effectiveDate,
            PositionUnitId: positionUnitId,
            Position: position.Trim(),
            Note: Normalize(note),
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));
    }

    public void ChangeTemporaryPosition(DateOnly effectiveDate, string? temporaryPosition, Guid? TemporaryPositionUnitId, string? note, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        Raise(new PersonTemporaryPositionChanged(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            EffectiveDate: effectiveDate,
            TemporaryPosition: Normalize(temporaryPosition),
            TemporaryPositionUnitId: TemporaryPositionUnitId,
            Note: Normalize(note),
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));
    }

    public void ChangeBzvp(DateOnly effectiveDate, string bzvp, string? note, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        if (string.IsNullOrWhiteSpace(bzvp))
            throw new ArgumentException("BZVP is required.", nameof(bzvp));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        Raise(new PersonBzvpChanged(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            EffectiveDate: effectiveDate,
            Bzvp: bzvp.Trim(),
            Note: Normalize(note),
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));
    }

    public void ChangeWeapon(DateOnly effectiveDate, string? weapon, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        Raise(new PersonWeaponChanged(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            EffectiveDate: effectiveDate,
            Weapon: Normalize(weapon),
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));
    }

    public void ChangeCallsign(DateOnly effectiveDate, string? callsign, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        Raise(new PersonCallsignChanged(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            EffectiveDate: effectiveDate,
            Callsign: Normalize(callsign),
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));
    }

    public void Enroll(
        EnrollmentKind kind,
        string? reference,
        string reason,
        DateOnly enrollDate,
        Guid PositionUnitId,
        string author,
        DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        if (Lifecycle != PersonLifecycle.Candidate)
            throw new InvalidOperationException("Зарахування можливе лише для кандидата.");

        if (Personal is null)
            throw new InvalidOperationException("Неможливо зарахувати без персональної інформації.");

        if (string.IsNullOrWhiteSpace(Rank))
            throw new InvalidOperationException("Неможливо зарахувати без звання.");

        if (PositionUnitId == Guid.Empty)
            throw new InvalidOperationException("Неможливо зарахувати без посади.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        Raise(new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            Kind: kind,
            Reference: Normalize(reference),
            Reason: reason.Trim(),
            EnrollDate: enrollDate,
            PositionUnitId: PositionUnitId,
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));
    }

    public void Exclude(string reason, DateOnly effectiveDate, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        Raise(new PersonExcluded(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            Reason: reason.Trim(),
            EffectiveDate: effectiveDate,
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));
    }

    public void VoidEvent(Guid targetEventId, string reason, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        if (targetEventId == Guid.Empty)
            throw new ArgumentException("TargetEventId is required.", nameof(targetEventId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        Raise(new PersonEventVoided(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            TargetEventId: targetEventId,
            Reason: reason.Trim(),
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));
    }

    // ============================
    // Replay from history (канонично)
    // ============================

    public void LoadFromHistory(IEnumerable<StoredEvent> history)
    {
        var events = history as IList<StoredEvent> ?? [.. history];

        ResetStateForReplay();
        _changes.Clear();            // канонично: rehydrate => никаких uncommitted
        _voidedEventIds.Clear();

        if (events.Count == 0)
        {
            Version = 0;
            return;
        }

        // 1) backward scan: определить итоговый набор voided, корректно для "void of void"
        // Правило: если событие само voided => оно не оказывает эффекта (включая void-события).
        var voided = new HashSet<Guid>();

        for (int i = events.Count - 1; i >= 0; i--)
        {
            var evt = events[i].Event;

            if (voided.Contains(evt.EventId))
                continue; // это событие "вырезано" более поздним void

            if (evt is PersonEventVoided v)
            {
                voided.Add(v.TargetEventId);
            }
        }

        _voidedEventIds.UnionWith(voided);

        // 2) forward apply: применяем только "живые" (не voided) и не void-события
        foreach (var se in events)
        {
            Version = se.Version; // stream version всегда двигается по записи, даже если evt voided

            var evt = se.Event;

            if (voided.Contains(evt.EventId))
                continue;

            if (evt is PersonEventVoided)
                continue;

            Apply(evt);
        }
    }

    private void ResetStateForReplay()
    {
        Id = Guid.Empty;
        Version = 0;

        Lifecycle = PersonLifecycle.Candidate;

        Personal = null;
        Rank = null;

        PlannedPositionUnitId = null;
        PlannedPosition = null;

        PositionUnitId = null;
        Position = null;

        TemporaryPosition = null;
        TemporaryPositionUnitId = null;

        BZVP = null;
        Weapon = null;
        Callsign = null;

        EnrollmentKind = null;
        EnrollmentReference = null;

        EnrolledAt = null;
        ExcludedAt = null;
    }

    // Apply должен быть чисто state mutation (без _changes)
    private void Apply(IDomainEvent e)
    {
        switch (e)
        {
            case PersonCandidateCreated x:
                Id = x.AggregateId;
                Lifecycle = PersonLifecycle.Candidate;
                Personal = x.Personal;
                PlannedPosition = Normalize(x.PlannedPosition);
                PlannedPositionUnitId = x.PlannedPositionUnitId; // NEW
                break;

            case PersonPersonalInfoUpdated x:
                Personal = x.Personal;
                PlannedPosition = Normalize(x.PlannedPosition);
                break;

            case PersonRankChanged x:
                Rank = x.Rank;
                break;

            case PersonPositionChanged x:
                PositionUnitId = x.PositionUnitId;
                Position = x.Position;
                break;

            case PersonTemporaryPositionChanged x:
                TemporaryPositionUnitId = x.TemporaryPositionUnitId;
                TemporaryPosition = Normalize(x.TemporaryPosition);
                break;

            case PersonBzvpChanged x:
                BZVP = x.Bzvp;
                break;

            case PersonWeaponChanged x:
                Weapon = Normalize(x.Weapon);
                break;

            case PersonCallsignChanged x:
                Callsign = Normalize(x.Callsign);
                break;

            case PersonEnrolled x:
                Lifecycle = PersonLifecycle.Enrolled;
                EnrolledAt = x.EnrollDate;
                EnrollmentKind = x.Kind;
                EnrollmentReference = Normalize(x.Reference);
                PositionUnitId = x.PositionUnitId;          // NEW
                PlannedPositionUnitId = null;               // логічно: резерв більше не потрібен
                break;

            case PersonExcluded x:
                Lifecycle = PersonLifecycle.Excluded;
                ExcludedAt = x.EffectiveDate;

                // можна лишити ids як історію, але практично краще занулити:
                PlannedPositionUnitId = null;
                TemporaryPositionUnitId = null;
                PositionUnitId = null;
                break;

                // PersonEventVoided не меняет state напрямую (state считается через rebuild/replay)
        }
    }

    private static string? Normalize(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private void EnsureInitialized()
    {
        if (Id == Guid.Empty)
            throw new InvalidOperationException("Агрегат не ініціалізований.");
    }

    private void EnsureNotExcluded()
    {
        if (Lifecycle == PersonLifecycle.Excluded)
            throw new InvalidOperationException("Особа виключена.");
    }
}
