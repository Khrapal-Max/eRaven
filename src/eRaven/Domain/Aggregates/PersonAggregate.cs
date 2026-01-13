//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonAggregate (simplified: Position is text only)
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Events.PersonEvents.Info;
using eRaven.Domain.Events.PersonEvents.Move;
using eRaven.Domain.ValueObjects;

namespace eRaven.Domain.Aggregates;

public sealed class PersonAggregate
{
    public readonly record struct StoredEvent(long Version, IDomainEvent Event);

    private readonly List<IDomainEvent> _changes = [];
    private readonly HashSet<Guid> _voidedEventIds = [];

    public Guid Id { get; private set; }
    public long Version { get; private set; }

    // ============================
    // Основні властивості
    // ============================

    public PersonLifecycle Lifecycle { get; private set; } = PersonLifecycle.Reserved;

    public PersonalInfo? Personal { get; private set; }
    public string? Rank { get; private set; }
    public int? PositionSort { get; private set; }
    public string? Position { get; private set; }

    public string? BZVP { get; private set; }
    public string? Weapon { get; private set; }
    public string? Callsign { get; private set; }

    public EnrollmentKind? EnrollmentKind { get; private set; }
    public string? EnrollmentReference { get; private set; }

    public DateOnly? EnrolledAt { get; private set; }
    public DateOnly? ExcludedAt { get; private set; } // останнє виключення (історія в стрімі)

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

    private void Raise(IDomainEvent e)
    {
        Apply(e);
        _changes.Add(e);
    }

    // ============================
    // Commands
    // ============================

    public static PersonAggregate CreateReserved(
        Guid id,
        PersonalInfo personal,
        string? rank,
        string? position,
        string author,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("AggregateId is required.", nameof(id));

        ArgumentNullException.ThrowIfNull(personal);

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        var a = new PersonAggregate();

        a.Raise(new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: personal,
            Rank: Normalize(rank),
            Position: Normalize(position),
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));

        return a;
    }

    public void UpdatePersonalInfo(PersonalInfo personal, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        ArgumentNullException.ThrowIfNull(personal);

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        Raise(new PersonPersonalInfoUpdated(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            Personal: personal,
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));
    }

    public void ChangeRank(DateOnly effectiveDate, string rank, string? note, string author, DateTime nowUtc)
    {
        EnsureInitialized();

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

    public void ChangePosition(DateOnly effectiveDate, int positionSort, string position, string? note, string author, DateTime nowUtc)
    {
        EnsureInitialized();

        if (positionSort <= 0)
            throw new ArgumentException("PositionSort must be > 0", nameof(positionSort));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        var evt = new PersonPositionChanged(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            EffectiveDate: effectiveDate,
            PositionSort: positionSort,
            Position: position.Trim(),
            Note: string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            Author: author.Trim(),
            OccurredAtUtc: nowUtc);

        Raise(evt);
    }

    public void ChangeBzvp(DateOnly effectiveDate, string bzvp, string? note, string author, DateTime nowUtc)
    {
        EnsureInitialized();

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
     string rank,
     int positionSort,
     string position,
     string author,
     DateTime nowUtc)
    {
        EnsureInitialized();

        if (Lifecycle == PersonLifecycle.Enrolled)
            throw new InvalidOperationException("Особа вже зарахована.");

        if (Personal is null)
            throw new InvalidOperationException("Неможливо зарахувати без персональної інформації.");

        if (string.IsNullOrWhiteSpace(rank))
            throw new InvalidOperationException("Неможливо зарахувати без звання.");

        if (string.IsNullOrWhiteSpace(position))
            throw new InvalidOperationException("Неможливо зарахувати без посади.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        if (ExcludedAt is DateOnly ex && enrollDate <= ex)
            throw new InvalidOperationException("Дата зарахування має бути пізніше дати виключення.");

        var evt = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            Kind: kind,
            Reference: Normalize(reference),
            Reason: reason.Trim(),
            EnrollDate: enrollDate,
            Rank: rank.Trim(),
            PositionSort: positionSort,
            Position: position.Trim(),
            Author: author.Trim(),
            OccurredAtUtc: nowUtc);

        Raise(evt);
    }

    public void Exclude(string reason, DateOnly effectiveDate, string author, DateTime nowUtc)
    {
        EnsureInitialized();

        if (Lifecycle != PersonLifecycle.Enrolled)
            throw new InvalidOperationException("Виключити можна лише зараховану особу.");

        if (EnrolledAt is DateOnly enrolledAt && effectiveDate < enrolledAt)
            throw new InvalidOperationException("Дата виключення не може бути раніше дати зарахування.");

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
    // Replay from history (void-aware)
    // ============================

    public void LoadFromHistory(IEnumerable<StoredEvent> history)
    {
        var events = history as IList<StoredEvent> ?? [.. history];

        ResetStateForReplay();
        _changes.Clear();
        _voidedEventIds.Clear();

        if (events.Count == 0)
        {
            Version = 0;
            return;
        }

        var voided = new HashSet<Guid>();

        for (int i = events.Count - 1; i >= 0; i--)
        {
            var evt = events[i].Event;

            if (voided.Contains(evt.EventId))
                continue;

            if (evt is PersonEventVoided v)
                voided.Add(v.TargetEventId);
        }

        _voidedEventIds.UnionWith(voided);

        foreach (var se in events)
        {
            Version = se.Version;

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

        Lifecycle = PersonLifecycle.Reserved;
        Personal = null;

        Rank = null;
        PositionSort = 0;
        Position = null;

        BZVP = null;
        Weapon = null;
        Callsign = null;

        EnrollmentKind = null;
        EnrollmentReference = null;

        EnrolledAt = null;
        ExcludedAt = null;
    }

    private void Apply(IDomainEvent e)
    {
        switch (e)
        {
            case PersonCreated x:
                Id = x.AggregateId;
                Lifecycle = PersonLifecycle.Reserved;

                Personal = x.Personal;

                Rank = Normalize(x.Rank);
                Position = Normalize(x.Position);

                EnrollmentKind = null;
                EnrollmentReference = null;
                EnrolledAt = null;
                break;

            case PersonPersonalInfoUpdated x:
                Personal = x.Personal;
                break;

            case PersonRankChanged x:
                Rank = x.Rank;
                break;

            case PersonPositionChanged x:
                Position = Normalize(x.Position);
                PositionSort = x.PositionSort;
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
                EnrollmentKind = x.Kind;
                EnrollmentReference = Normalize(x.Reference);
                EnrolledAt = x.EnrollDate;
                Rank = x.Rank;
                PositionSort = x.PositionSort;
                Position = x.Position;
                ExcludedAt = null;
                break;

            case PersonExcluded x:
                Lifecycle = PersonLifecycle.Reserved;
                EnrollmentKind = null;
                EnrollmentReference = null;
                ExcludedAt = x.EffectiveDate;
                break;
        }
    }

    private static string? Normalize(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private void EnsureInitialized()
    {
        if (Id == Guid.Empty)
            throw new InvalidOperationException("Агрегат не ініціалізований.");
    }
}
