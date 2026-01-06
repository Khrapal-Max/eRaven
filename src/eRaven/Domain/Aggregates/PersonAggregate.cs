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

    public DateOnly? EnrolledAt { get; private set; }
    public DateOnly? ExcludedAt { get; private set; }

    // ============================
    // Void API
    // ============================

    public void VoidEvent(Guid targetEventId, string reason, string author, DateTime nowUtc)
    {
        EnsureInitialized();
        EnsureNotExcluded();

        ApplyChange(new PersonEventVoided(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            TargetEventId: targetEventId,
            Reason: reason,
            Author: author,
            OccurredAtUtc: nowUtc
        ));
    }

    // ============================
    // ES plumbing
    // ============================

    public IReadOnlyList<IDomainEvent> GetUncommittedChanges() => _changes;
    public void ClearUncommittedChanges() => _changes.Clear();

    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        foreach (var e in history.OrderBy(x => x.OccurredAtUtc))
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
        // 1️⃣ Voided events – завжди обробляємо
        if (e is PersonEventVoided v)
        {
            _voidedEventIds.Add(v.TargetEventId);
            return;
        }

        // 2️⃣ Якщо подія void → ігноруємо
        if (_voidedEventIds.Contains(e.EventId))
            return;

        // 3️⃣ Звичайний replay
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
            throw new InvalidOperationException("Агрегат не ініціалізований.");
    }

    private void EnsureNotExcluded()
    {
        if (Lifecycle == PersonLifecycle.Excluded)
            throw new InvalidOperationException("Особа виключена.");
    }
}
