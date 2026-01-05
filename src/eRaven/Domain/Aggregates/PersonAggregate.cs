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

    public Guid Id { get; private set; }
    public long Version { get; private set; }

    public PersonLifecycle Lifecycle { get; private set; } = PersonLifecycle.Candidate;

    // Candidate base
    public PersonalInfo? Personal { get; private set; }
    public string? PlannedPosition { get; private set; }

    // Key points (для Enrolled)
    public string? Rank { get; private set; }
    public string? Position { get; private set; }
    public string? TemporaryPosition { get; private set; }

    public string? BZVP { get; private set; }
    public string? Weapon { get; private set; }
    public string? Callsign { get; private set; }

    public DateOnly? EnrolledAt { get; private set; }
    public DateOnly? ExcludedAt { get; private set; }

    public IReadOnlyList<IDomainEvent> GetUncommittedChanges() => _changes;
    public void ClearUncommittedChanges() => _changes.Clear();

    // ===== Factory: base point =====

    /// <summary>
    /// Створення картки кандидата
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static PersonAggregate CreateCandidate(Guid id, PersonalInfo personal, string? plannedPosition, string author, DateTime nowUtc)
    {
        var a = new PersonAggregate();
        a.ApplyChange(new PersonCandidateCreated(
            AggregateId: id,
            Personal: personal ?? throw new ArgumentNullException(nameof(personal)),
            PlannedPosition: Normalize(plannedPosition),
            Author: author,
            OccurredAtUtc: nowUtc));

        return a;
    }

    // ===== Edit =====
    /// <summary>
    /// Оновлення персональної інформації кандидата/особи
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public void UpdatePersonalInfo(PersonalInfo personal, string? plannedPosition, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        ApplyChange(new PersonPersonalInfoUpdated(
            AggregateId: Id,
            Personal: personal ?? throw new ArgumentNullException(nameof(personal)),
            PlannedPosition: Normalize(plannedPosition),
            Author: author,
            OccurredAtUtc: nowUtc));
    }

    // ===== Key points changes =====
    /// <summary>
    /// Зміна звання особи
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public void ChangeRank(DateOnly effectiveDate, string rank, string? note, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        // ви можете дозволити виставляти rank ще на Candidate (щоб потім Enroll пройшов)
        if (string.IsNullOrWhiteSpace(rank))
            throw new ArgumentException("Звання обов'язкове", nameof(rank));

        ApplyChange(new PersonRankChanged(Id, effectiveDate, rank.Trim(), Normalize(note), author, nowUtc));
    }

    /// <summary>
    /// Зміна посади особи
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public void ChangePosition(DateOnly effectiveDate, string position, string? note, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        if (string.IsNullOrWhiteSpace(position))
            throw new ArgumentException("Посада обов'язкова", nameof(position));

        ApplyChange(new PersonPositionChanged(Id, effectiveDate, position.Trim(), Normalize(note), author, nowUtc));
    }

    /// <summary>
    /// Призначення або зміна тимчасової посади особи
    /// </summary>
    public void ChangeTemporaryPosition(DateOnly effectiveDate, string? temporaryPosition, string? note, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        ApplyChange(new PersonTemporaryPositionChanged(Id, effectiveDate, Normalize(temporaryPosition), Normalize(note), author, nowUtc));
    }

    /// <summary>
    /// Зміна БЗВП особи
    /// </summary>
    public void ChangeBzvp(DateOnly effectiveDate, string bzvp, string? note, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        if (string.IsNullOrWhiteSpace(bzvp))
            throw new ArgumentException("БЗВП обов'язковий", nameof(bzvp));

        ApplyChange(new PersonBzvpChanged(Id, effectiveDate, bzvp.Trim(), Normalize(note), author, nowUtc));
    }

    /// <summary>
    /// Зміна зброї особи
    /// </summary>
    public void ChangeWeapon(DateOnly effectiveDate, string? weapon, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();
        ApplyChange(new PersonWeaponChanged(Id, effectiveDate, Normalize(weapon), author, nowUtc));
    }

    /// <summary>
    /// Зміна позивного особи
    /// </summary>
    public void ChangeCallsign(DateOnly effectiveDate, string? callsign, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();
        ApplyChange(new PersonCallsignChanged(Id, effectiveDate, Normalize(callsign), author, nowUtc));
    }

    // ===== Lifecycle transitions =====
    /// <summary>
    /// Зарахування кандидата
    /// </summary>
    public void Enroll(string reason, DateOnly enrollDate, string author, DateTime nowUtc)
    {
        EnsureInitialized();

        if (Lifecycle != PersonLifecycle.Candidate)
            throw new InvalidOperationException("Зарахування можливе лише для кандидата.");

        if (Personal is null)
            throw new InvalidOperationException("Неможливо зарахувати без персональної інформації.");

        // ваша вимога:
        if (string.IsNullOrWhiteSpace(Rank))
            throw new InvalidOperationException("Неможливо зарахувати без звання.");

        if (string.IsNullOrWhiteSpace(Position))
            throw new InvalidOperationException("Неможливо зарахувати без посади.");

        ApplyChange(new PersonEnrolled(Id, reason, enrollDate, author, nowUtc));
    }

    /// <summary>
    /// Виключення/звільнення особи
    /// </summary>
    public void Exclude(string reason, DateOnly effectiveDate, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();
        ApplyChange(new PersonExcluded(Id, reason, effectiveDate, author, nowUtc));
    }

    // ===== ES plumbing =====

    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        foreach (var e in history)
        {
            Apply(e);
            Version++;
        }
    }

    private void ApplyChange(IDomainEvent e)
    {
        Apply(e);
        _changes.Add(e);
    }

    private void Apply(IDomainEvent e)
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
            throw new InvalidOperationException("Агрегат не ініціалізований. Використайте CreateCandidate або LoadFromHistory.");
    }

    private void EnsureNotExcluded()
    {
        if (Lifecycle == PersonLifecycle.Excluded)
            throw new InvalidOperationException("Особа виключена/звільнена.");
    }
}
