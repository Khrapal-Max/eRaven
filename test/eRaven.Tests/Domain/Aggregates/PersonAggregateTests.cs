//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonAggregateTests (updated for canonical ES aggregate)
//-----------------------------------------------------------------------------

using eRaven.Domain;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Enums;
using eRaven.Domain.Events;
using eRaven.Domain.ValueObjects;

namespace eRaven.Tests.Domain.Aggregates;

public sealed class PersonAggregateTests
{
    private static readonly DateTime NowUtc = new(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc);

    private static PersonalInfo Personal(string lastName = "Ivanov", string firstName = "Ivan", string? middle = "Ivanovich")
        => new("1234567890", lastName, firstName, middle);

    private static PersonAggregate.StoredEvent SE(long version, IDomainEvent evt)
        => new(version, evt);

    // =========================
    // replay
    // =========================

    [Fact]
    public void LoadFromHistory_when_candidate_created_should_initialize_state_and_set_stream_version()
    {
        var id = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: " Planned ",
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var sut = new PersonAggregate();

        sut.LoadFromHistory([SE(1, created)]);

        Assert.Equal(id, sut.Id);
        Assert.Equal(1, sut.Version); // stream version

        Assert.Equal(PersonLifecycle.Candidate, sut.Lifecycle);

        Assert.NotNull(sut.Personal);
        Assert.Equal("1234567890", sut.Personal!.Rnokpp);
        Assert.Equal("Ivanov", sut.Personal.LastName);
        Assert.Equal("Ivan", sut.Personal.FirstName);
        Assert.Equal("Ivanovich", sut.Personal.MiddleName);
        Assert.Equal("Ivanov Ivan Ivanovich", sut.Personal.FullName);

        Assert.Equal("Planned", sut.PlannedPosition);

        // key points empty
        Assert.Null(sut.Rank);
        Assert.Null(sut.Position);
        Assert.Null(sut.TemporaryPosition);
        Assert.Null(sut.BZVP);
        Assert.Null(sut.Weapon);
        Assert.Null(sut.Callsign);

        Assert.Null(sut.EnrolledAt);
        Assert.Null(sut.ExcludedAt);
        Assert.Null(sut.EnrollmentKind);
        Assert.Null(sut.EnrollmentReference);

        // rehydrate must not create uncommitted changes
        Assert.Empty(sut.GetUncommittedChanges());
    }

    [Fact]
    public void LoadFromHistory_when_event_voided_should_ignore_that_event_on_replay_but_keep_stream_version()
    {
        var id = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var rankEventId = Guid.NewGuid();
        var rankChanged = new PersonRankChanged(
            EventId: rankEventId,
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 01),
            Rank: "Солдат",
            Note: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(2));

        var voided = new PersonEventVoided(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            TargetEventId: rankEventId,
            Reason: "wrong rank",
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(3));

        var sut = new PersonAggregate();

        sut.LoadFromHistory([
            SE(1, created),
            SE(2, rankChanged),
            SE(3, voided)
        ]);

        Assert.Equal(PersonLifecycle.Candidate, sut.Lifecycle);
        Assert.Null(sut.Rank);       // rank was voided
        Assert.Equal(3, sut.Version); // ✅ stream version = last record version (including void)
    }

    [Fact]
    public void LoadFromHistory_when_enrolled_should_set_enrollment_fields_and_stream_version()
    {
        var id = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var rank = new PersonRankChanged(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 01),
            Rank: "Сержант",
            Note: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(2));

        var pos = new PersonPositionChanged(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 01),
            Position: "Стрілець",
            Note: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(3));

        var enrolled = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.AttachedByOrder,
            Reference: "№123",
            Reason: "Наказ",
            EnrollDate: new DateOnly(2026, 01, 10),
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(4));

        var sut = new PersonAggregate();
        sut.LoadFromHistory([
            SE(1, created),
            SE(2, rank),
            SE(3, pos),
            SE(4, enrolled)
        ]);

        Assert.Equal(4, sut.Version); // stream version
        Assert.Equal(PersonLifecycle.Enrolled, sut.Lifecycle);
        Assert.Equal(new DateOnly(2026, 01, 10), sut.EnrolledAt);
        Assert.Equal(EnrollmentKind.AttachedByOrder, sut.EnrollmentKind);
        Assert.Equal("№123", sut.EnrollmentReference);
    }

    [Fact]
    public void LoadFromHistory_void_of_void_should_restore_original_event_effect()
    {
        var id = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var rankEventId = Guid.NewGuid();
        var rankChanged = new PersonRankChanged(
            EventId: rankEventId,
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 01),
            Rank: "Солдат",
            Note: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var voidId = Guid.NewGuid();
        var voidedRank = new PersonEventVoided(
            EventId: voidId,
            AggregateId: id,
            TargetEventId: rankEventId,
            Reason: "wrong rank",
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(2));

        // void of void
        var voidedVoid = new PersonEventVoided(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            TargetEventId: voidId,
            Reason: "restore",
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(3));

        var sut = new PersonAggregate();
        sut.LoadFromHistory([
            SE(1, created),
            SE(2, rankChanged),
            SE(3, voidedRank),
            SE(4, voidedVoid)
        ]);

        // ✅ rank должен вернуться, потому что voidedRank сам вырезан
        Assert.Equal("Солдат", sut.Rank);
        Assert.Equal(4, sut.Version);
    }

    // =========================
    // commands (uncommitted events + validations)
    // =========================

    [Fact]
    public void CreateCandidate_should_produce_uncommitted_event_and_apply_state_immediately()
    {
        var id = Guid.NewGuid();

        var sut = PersonAggregate.CreateCandidate(
            id: id,
            personal: Personal(),
            plannedPosition: " Planned ",
            author: " tester ",
            nowUtc: NowUtc);

        // state applied (canonical)
        Assert.Equal(id, sut.Id);
        Assert.Equal(PersonLifecycle.Candidate, sut.Lifecycle);
        Assert.Equal("Planned", sut.PlannedPosition);

        var changes = sut.GetUncommittedChanges();
        Assert.Single(changes);

        var evt = Assert.IsType<PersonCandidateCreated>(changes[0]);
        Assert.Equal(id, evt.AggregateId);
        Assert.Equal("tester", evt.Author);
        Assert.Equal("Planned", evt.PlannedPosition);
        Assert.Equal("1234567890", evt.Personal.Rnokpp);
    }

    [Fact]
    public void UpdatePersonalInfo_when_excluded_should_throw()
    {
        var id = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var excluded = new PersonExcluded(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Reason: "excluded",
            EffectiveDate: new DateOnly(2026, 01, 02),
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var sut = new PersonAggregate();
        sut.LoadFromHistory([
            SE(1, created),
            SE(2, excluded)
        ]);

        Assert.Throws<InvalidOperationException>(() =>
            sut.UpdatePersonalInfo(Personal(lastName: "NEW"), null, "tester", NowUtc.AddMinutes(2)));
    }

    [Fact]
    public void Enroll_when_missing_rank_should_throw()
    {
        var id = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var pos = new PersonPositionChanged(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 01),
            Position: "Стрілець",
            Note: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var sut = new PersonAggregate();
        sut.LoadFromHistory([
            SE(1, created),
            SE(2, pos)
        ]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.Enroll(
                kind: EnrollmentKind.Unit,
                reference: null,
                reason: "ok",
                enrollDate: new DateOnly(2026, 01, 10),
                author: "tester",
                nowUtc: NowUtc.AddMinutes(2)));

        Assert.Contains("без звання", ex.Message);
    }

    [Fact]
    public void Enroll_when_missing_position_should_throw()
    {
        var id = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var rank = new PersonRankChanged(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 01),
            Rank: "Солдат",
            Note: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var sut = new PersonAggregate();
        sut.LoadFromHistory([
            SE(1, created),
            SE(2, rank)
        ]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.Enroll(
                kind: EnrollmentKind.Unit,
                reference: null,
                reason: "ok",
                enrollDate: new DateOnly(2026, 01, 10),
                author: "tester",
                nowUtc: NowUtc.AddMinutes(2)));

        Assert.Contains("без посади", ex.Message);
    }

    [Fact]
    public void Enroll_when_ok_should_add_uncommitted_event_with_all_fields_and_apply_state()
    {
        var id = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var rank = new PersonRankChanged(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 01),
            Rank: "Сержант",
            Note: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var pos = new PersonPositionChanged(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 01),
            Position: "Оператор",
            Note: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(2));

        var sut = new PersonAggregate();
        sut.LoadFromHistory([
            SE(1, created),
            SE(2, rank),
            SE(3, pos)
        ]);

        sut.Enroll(
            kind: EnrollmentKind.AttachedByList,
            reference: " List-55 ",
            reason: "Test reason",
            enrollDate: new DateOnly(2026, 01, 10),
            author: "tester",
            nowUtc: NowUtc.AddMinutes(3));

        // state applied immediately
        Assert.Equal(PersonLifecycle.Enrolled, sut.Lifecycle);
        Assert.Equal(new DateOnly(2026, 01, 10), sut.EnrolledAt);
        Assert.Equal(EnrollmentKind.AttachedByList, sut.EnrollmentKind);
        Assert.Equal("List-55", sut.EnrollmentReference);

        var changes = sut.GetUncommittedChanges();
        Assert.Single(changes);

        var evt = Assert.IsType<PersonEnrolled>(changes[0]);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.Equal(id, evt.AggregateId);
        Assert.Equal(EnrollmentKind.AttachedByList, evt.Kind);
        Assert.Equal("List-55", evt.Reference);
        Assert.Equal("Test reason", evt.Reason);
        Assert.Equal(new DateOnly(2026, 01, 10), evt.EnrollDate);
        Assert.Equal("tester", evt.Author);
    }

    [Fact]
    public void Exclude_should_add_uncommitted_event_and_apply_state_immediately()
    {
        var id = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created)]);

        sut.Exclude("reason", new DateOnly(2026, 01, 11), "tester", NowUtc.AddMinutes(1));

        // state applied immediately
        Assert.Equal(PersonLifecycle.Excluded, sut.Lifecycle);
        Assert.Equal(new DateOnly(2026, 01, 11), sut.ExcludedAt);

        var change = Assert.Single(sut.GetUncommittedChanges());
        var evt = Assert.IsType<PersonExcluded>(change);

        Assert.Equal(id, evt.AggregateId);
        Assert.Equal("reason", evt.Reason);
        Assert.Equal(new DateOnly(2026, 01, 11), evt.EffectiveDate);

        // replay should set excluded and keep stream version
        var sut2 = new PersonAggregate();
        sut2.LoadFromHistory([
            SE(1, created),
            SE(2, evt)
        ]);

        Assert.Equal(2, sut2.Version);
        Assert.Equal(PersonLifecycle.Excluded, sut2.Lifecycle);
        Assert.Equal(new DateOnly(2026, 01, 11), sut2.ExcludedAt);
    }

    [Fact]
    public void VoidEvent_should_add_uncommitted_change_and_validate_inputs()
    {
        var id = Guid.NewGuid();
        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created)]);

        Assert.Throws<ArgumentException>(() => sut.VoidEvent(Guid.Empty, "x", "tester", NowUtc));
        Assert.Throws<ArgumentException>(() => sut.VoidEvent(Guid.NewGuid(), "", "tester", NowUtc));
        Assert.Throws<ArgumentException>(() => sut.VoidEvent(Guid.NewGuid(), "x", "", NowUtc));

        var targetId = Guid.NewGuid();
        sut.VoidEvent(targetId, " fix ", " tester ", NowUtc.AddMinutes(1));

        var change = Assert.Single(sut.GetUncommittedChanges());
        var evt = Assert.IsType<PersonEventVoided>(change);

        Assert.Equal(id, evt.AggregateId);
        Assert.Equal(targetId, evt.TargetEventId);
        Assert.Equal("fix", evt.Reason);
        Assert.Equal("tester", evt.Author);
    }

    [Fact]
    public void LoadFromHistory_should_clear_uncommitted_changes()
    {
        var id = Guid.NewGuid();

        var sut = PersonAggregate.CreateCandidate(id, Personal(), null, "tester", NowUtc);
        Assert.Single(sut.GetUncommittedChanges());

        // rehydrate with empty history => clear changes and reset
        sut.LoadFromHistory(Array.Empty<PersonAggregate.StoredEvent>());

        Assert.Empty(sut.GetUncommittedChanges());
        Assert.Equal(Guid.Empty, sut.Id);
        Assert.Equal(0, sut.Version);
    }
}
