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
    private readonly List<IDomainEvent> _changes = [];
    private readonly HashSet<Guid> _voidedEventIds = [];

    public Guid Id { get; private set; }
    public long Version { get; private set; }

    public PersonLifecycle Lifecycle { get; private set; } = PersonLifecycle.Candidate;

    public PersonalInfo? Personal { get; private set; }
    public string? PlannedPosition { get; private set; }

    public string? Rank { get; private set; }
    public string? Position { get; private set; }
    public string? TemporaryPosition { get; private set; }
    public string? BZVP { get; private set; }
    public string? Weapon { get; private set; }
    public string? Callsign { get; private set; }

    // Enrollment (потрібно для звітів/групувань)
    public EnrollmentKind? EnrollmentKind { get; private set; }
    public string? EnrollmentReference { get; private set; }

    public DateOnly? EnrolledAt { get; private set; }
    public DateOnly? ExcludedAt { get; private set; }

    // ============================
    // Uncommitted changes
    // ============================

    public IReadOnlyList<IDomainEvent> GetUncommittedChanges() => _changes;
    public void ClearUncommittedChanges() => _changes.Clear();

    private void ApplyChange(IDomainEvent e)
    {
        // Команди не перераховують стан в пам’яті.
        // Стан — тільки через replay (LoadFromHistory) або read-model.
        _changes.Add(e);
    }

    // ============================
    // Commands (для хендлерів)
    // ============================

    public static PersonAggregate CreateCandidate(
        Guid id,
        PersonalInfo personal,
        string? plannedPosition,
        string author,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("AggregateId is required.", nameof(id));

        ArgumentNullException.ThrowIfNull(personal);

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        var a = new PersonAggregate();

        a.ApplyChange(new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: personal,
            PlannedPosition: Normalize(plannedPosition),
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

        ApplyChange(new PersonPersonalInfoUpdated(
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

        ApplyChange(new PersonRankChanged(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            EffectiveDate: effectiveDate,
            Rank: rank.Trim(),
            Note: Normalize(note),
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));
    }

    public void ChangePosition(DateOnly effectiveDate, string position, string? note, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        if (string.IsNullOrWhiteSpace(position))
            throw new ArgumentException("Position is required.", nameof(position));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        ApplyChange(new PersonPositionChanged(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            EffectiveDate: effectiveDate,
            Position: position.Trim(),
            Note: Normalize(note),
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));
    }

    public void ChangeTemporaryPosition(DateOnly effectiveDate, string? temporaryPosition, string? note, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        ApplyChange(new PersonTemporaryPositionChanged(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            EffectiveDate: effectiveDate,
            TemporaryPosition: Normalize(temporaryPosition),
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

        ApplyChange(new PersonBzvpChanged(
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

        ApplyChange(new PersonWeaponChanged(
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

        ApplyChange(new PersonCallsignChanged(
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

        if (string.IsNullOrWhiteSpace(Position))
            throw new InvalidOperationException("Неможливо зарахувати без посади.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        ApplyChange(new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            Kind: kind,
            Reference: Normalize(reference),
            Reason: reason.Trim(),
            EnrollDate: enrollDate,
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

        ApplyChange(new PersonExcluded(
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

        ApplyChange(new PersonEventVoided(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            TargetEventId: targetEventId,
            Reason: reason.Trim(),
            Author: author.Trim(),
            OccurredAtUtc: nowUtc
        ));
    }

    // ============================
    // Replay from history
    // ============================

    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        // Порядок задає EventStore (Version у PersonEventRecord), тут НЕ сортуємо.
        var events = history as IList<IDomainEvent> ?? [.. history];

        _voidedEventIds.Clear();
        foreach (var e in events)
        {
            if (e is PersonEventVoided v)
                _voidedEventIds.Add(v.TargetEventId);
        }

        ResetStateForReplay();

        foreach (var e in events)
        {
            if (e is PersonEventVoided)
                continue;

            if (_voidedEventIds.Contains(e.EventId))
                continue;

            ApplyNonVoided(e);
            Version++;
        }
    }

    private void ResetStateForReplay()
    {
        Id = Guid.Empty;
        Version = 0;

        Lifecycle = PersonLifecycle.Candidate;

        Personal = null;
        PlannedPosition = null;

        Rank = null;
        Position = null;
        TemporaryPosition = null;
        BZVP = null;
        Weapon = null;
        Callsign = null;

        EnrollmentKind = null;
        EnrollmentReference = null;

        EnrolledAt = null;
        ExcludedAt = null;

        // ⚠️ _changes НЕ чіпаємо
    }

    private void ApplyNonVoided(IDomainEvent e)
    {
        switch (e)
        {
            case PersonCandidateCreated x:
                Id = x.AggregateId;
                Lifecycle = PersonLifecycle.Candidate;
                Personal = x.Personal;
                PlannedPosition = Normalize(x.PlannedPosition);
                break;

            case PersonPersonalInfoUpdated x:
                Personal = x.Personal;
                PlannedPosition = Normalize(x.PlannedPosition);
                break;

            case PersonRankChanged x:
                Rank = x.Rank;
                break;

            case PersonPositionChanged x:
                Position = x.Position;
                break;

            case PersonTemporaryPositionChanged x:
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
                break;

            case PersonExcluded x:
                Lifecycle = PersonLifecycle.Excluded;
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

    private void EnsureNotExcluded()
    {
        if (Lifecycle == PersonLifecycle.Excluded)
            throw new InvalidOperationException("Особа виключена.");
    }
}
