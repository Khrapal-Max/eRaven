//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonAggregateTests (updated for PositionUnitIds + planned reserve)
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
        var plannedUnitId = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: " Planned ",
            PlannedPositionUnitId: plannedUnitId,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created)]);

        Assert.Equal(id, sut.Id);
        Assert.Equal(1, sut.Version);
        Assert.Equal(PersonLifecycle.Candidate, sut.Lifecycle);

        Assert.NotNull(sut.Personal);
        Assert.Equal("1234567890", sut.Personal!.Rnokpp);
        Assert.Equal("Ivanov", sut.Personal.LastName);
        Assert.Equal("Ivan", sut.Personal.FirstName);
        Assert.Equal("Ivanovich", sut.Personal.MiddleName);
        Assert.Equal("Ivanov Ivan Ivanovich", sut.Personal.FullName);

        Assert.Equal("Planned", sut.PlannedPosition);
        Assert.Equal(plannedUnitId, sut.PlannedPositionUnitId);

        // key points empty
        Assert.Null(sut.Rank);

        Assert.Null(sut.PositionUnitId);
        Assert.Null(sut.Position);

        Assert.Null(sut.TemporaryPositionUnitId);
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
            PlannedPositionUnitId: null,
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
        Assert.Null(sut.Rank);        // rank was voided
        Assert.Equal(3, sut.Version); // stream version = last record version (including void)
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
            PlannedPositionUnitId: null,
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

        Assert.Equal("Солдат", sut.Rank);
        Assert.Equal(4, sut.Version);
    }

    [Fact]
    public void LoadFromHistory_when_enrolled_should_set_enrollment_fields_and_clear_planned_reserve()
    {
        var id = Guid.NewGuid();
        var reserved = Guid.NewGuid();
        var mainPosId = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: "P",
            PlannedPositionUnitId: reserved,
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

        // позицию можно дать заранее через PositionChanged (теперь с PositionUnitId)
        var pos = new PersonPositionChanged(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 01),
            PositionUnitId: mainPosId,
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
            PositionUnitId: mainPosId,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(4));

        var sut = new PersonAggregate();
        sut.LoadFromHistory([
            SE(1, created),
            SE(2, rank),
            SE(3, pos),
            SE(4, enrolled)
        ]);

        Assert.Equal(4, sut.Version);
        Assert.Equal(PersonLifecycle.Enrolled, sut.Lifecycle);
        Assert.Equal(new DateOnly(2026, 01, 10), sut.EnrolledAt);

        Assert.Equal(EnrollmentKind.AttachedByOrder, sut.EnrollmentKind);
        Assert.Equal("№123", sut.EnrollmentReference);

        Assert.Equal(mainPosId, sut.PositionUnitId);
        Assert.Equal("Стрілець", sut.Position);

        Assert.Null(sut.PlannedPositionUnitId); // ✅ cleared in Apply(PersonEnrolled)
    }

    // =========================
    // commands (uncommitted events + validations)
    // =========================

    [Fact]
    public void CreateCandidate_should_produce_uncommitted_event_and_apply_state_immediately()
    {
        var id = Guid.NewGuid();
        var plannedUnitId = Guid.NewGuid();

        var sut = PersonAggregate.CreateCandidate(
            id: id,
            personal: Personal(),
            plannedPosition: " Planned ",
            plannedPositionUnitId: plannedUnitId,
            author: " tester ",
            nowUtc: NowUtc);

        // state applied
        Assert.Equal(id, sut.Id);
        Assert.Equal(PersonLifecycle.Candidate, sut.Lifecycle);
        Assert.Equal("Planned", sut.PlannedPosition);
        Assert.Equal(plannedUnitId, sut.PlannedPositionUnitId);

        var changes = sut.GetUncommittedChanges();
        Assert.Single(changes);

        var evt = Assert.IsType<PersonCandidateCreated>(changes[0]);
        Assert.Equal(id, evt.AggregateId);
        Assert.Equal("tester", evt.Author);
        Assert.Equal("Planned", evt.PlannedPosition);
        Assert.Equal(plannedUnitId, evt.PlannedPositionUnitId);
        Assert.Equal("1234567890", evt.Personal.Rnokpp);
    }

    [Fact]
    public void ChangePosition_should_require_position_unit_id_and_raise_event()
    {
        var id = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: null,
            PlannedPositionUnitId: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created)]);

        Assert.Throws<ArgumentException>(() =>
            sut.ChangePosition(new DateOnly(2026, 01, 01), Guid.Empty, "X", null, "tester", NowUtc.AddMinutes(1)));

        var posId = Guid.NewGuid();
        sut.ChangePosition(new DateOnly(2026, 01, 01), posId, " Оператор ", null, " tester ", NowUtc.AddMinutes(1));

        Assert.Equal(posId, sut.PositionUnitId);
        Assert.Equal("Оператор", sut.Position);

        var evt = Assert.IsType<PersonPositionChanged>(Assert.Single(sut.GetUncommittedChanges()));
        Assert.Equal(posId, evt.PositionUnitId);
        Assert.Equal("Оператор", evt.Position);
        Assert.Equal("tester", evt.Author);
    }

    [Fact]
    public void Enroll_when_missing_rank_should_throw()
    {
        var id = Guid.NewGuid();
        var posId = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: null,
            PlannedPositionUnitId: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        // позиция установлена, но rank нет
        var pos = new PersonPositionChanged(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 01),
            PositionUnitId: posId,
            Position: "Стрілець",
            Note: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created), SE(2, pos)]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.Enroll(
                kind: EnrollmentKind.Unit,
                reference: null,
                reason: "ok",
                enrollDate: new DateOnly(2026, 01, 10),
                PositionUnitId: posId,
                author: "tester",
                nowUtc: NowUtc.AddMinutes(2)));

        Assert.Contains("без звання", ex.Message);
    }

    [Fact]
    public void Enroll_when_missing_position_unit_id_should_throw()
    {
        var id = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: null,
            PlannedPositionUnitId: null,
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
        sut.LoadFromHistory([SE(1, created), SE(2, rank)]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.Enroll(
                kind: EnrollmentKind.Unit,
                reference: null,
                reason: "ok",
                enrollDate: new DateOnly(2026, 01, 10),
                PositionUnitId: Guid.Empty, // ✅ now this is the real "no position"
                author: "tester",
                nowUtc: NowUtc.AddMinutes(2)));

        Assert.Contains("без посади", ex.Message);
    }

    [Fact]
    public void Enroll_when_ok_should_add_uncommitted_event_with_all_fields_and_apply_state()
    {
        var id = Guid.NewGuid();
        var reserved = Guid.NewGuid();
        var mainPosId = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: "P",
            PlannedPositionUnitId: reserved,
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

        // установим должность (unit id)
        var pos = new PersonPositionChanged(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 01),
            PositionUnitId: mainPosId,
            Position: "Оператор",
            Note: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(2));

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created), SE(2, rank), SE(3, pos)]);

        sut.Enroll(
            kind: EnrollmentKind.AttachedByList,
            reference: " List-55 ",
            reason: "Test reason",
            enrollDate: new DateOnly(2026, 01, 10),
            PositionUnitId: mainPosId,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(3));

        Assert.Equal(PersonLifecycle.Enrolled, sut.Lifecycle);
        Assert.Equal(new DateOnly(2026, 01, 10), sut.EnrolledAt);
        Assert.Equal(EnrollmentKind.AttachedByList, sut.EnrollmentKind);
        Assert.Equal("List-55", sut.EnrollmentReference);

        Assert.Equal(mainPosId, sut.PositionUnitId);
        Assert.Null(sut.PlannedPositionUnitId); // ✅ reserve cleared on enroll

        var evt = Assert.IsType<PersonEnrolled>(Assert.Single(sut.GetUncommittedChanges()));
        Assert.Equal(id, evt.AggregateId);
        Assert.Equal(EnrollmentKind.AttachedByList, evt.Kind);
        Assert.Equal("List-55", evt.Reference);
        Assert.Equal("Test reason", evt.Reason);
        Assert.Equal(new DateOnly(2026, 01, 10), evt.EnrollDate);
        Assert.Equal(mainPosId, evt.PositionUnitId);
        Assert.Equal("tester", evt.Author);
    }

    [Fact]
    public void Exclude_should_add_uncommitted_event_and_apply_state_immediately_and_clear_position_ids()
    {
        var id = Guid.NewGuid();
        var planned = Guid.NewGuid();
        var main = Guid.NewGuid();
        var temp = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: "P",
            PlannedPositionUnitId: planned,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created)]);

        // имитируем состояние с занятыми id (как будто уже были события)
        sut.ChangePosition(new DateOnly(2026, 01, 01), main, "Оператор", null, "tester", NowUtc.AddMinutes(1));
        sut.ChangeTemporaryPosition(new DateOnly(2026, 01, 02), "В.о.", temp, null, "tester", NowUtc.AddMinutes(2));

        sut.ClearUncommittedChanges(); // чтобы тест проверял только Exclude

        sut.Exclude("reason", new DateOnly(2026, 01, 11), "tester", NowUtc.AddMinutes(3));

        Assert.Equal(PersonLifecycle.Excluded, sut.Lifecycle);
        Assert.Equal(new DateOnly(2026, 01, 11), sut.ExcludedAt);

        Assert.Null(sut.PlannedPositionUnitId);
        Assert.Null(sut.PositionUnitId);
        Assert.Null(sut.TemporaryPositionUnitId);

        var evt = Assert.IsType<PersonExcluded>(Assert.Single(sut.GetUncommittedChanges()));
        Assert.Equal("reason", evt.Reason);
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
            PlannedPositionUnitId: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created)]);

        Assert.Throws<ArgumentException>(() => sut.VoidEvent(Guid.Empty, "x", "tester", NowUtc));
        Assert.Throws<ArgumentException>(() => sut.VoidEvent(Guid.NewGuid(), "", "tester", NowUtc));
        Assert.Throws<ArgumentException>(() => sut.VoidEvent(Guid.NewGuid(), "x", "", NowUtc));

        var targetId = Guid.NewGuid();
        sut.VoidEvent(targetId, " fix ", " tester ", NowUtc.AddMinutes(1));

        var evt = Assert.IsType<PersonEventVoided>(Assert.Single(sut.GetUncommittedChanges()));
        Assert.Equal(id, evt.AggregateId);
        Assert.Equal(targetId, evt.TargetEventId);
        Assert.Equal("fix", evt.Reason);
        Assert.Equal("tester", evt.Author);
    }

    [Fact]
    public void LoadFromHistory_should_clear_uncommitted_changes()
    {
        var id = Guid.NewGuid();

        var sut = PersonAggregate.CreateCandidate(
            id: id,
            personal: Personal(),
            plannedPosition: null,
            plannedPositionUnitId: null,
            author: "tester",
            nowUtc: NowUtc);

        Assert.Single(sut.GetUncommittedChanges());

        sut.LoadFromHistory(Array.Empty<PersonAggregate.StoredEvent>());

        Assert.Empty(sut.GetUncommittedChanges());
        Assert.Equal(Guid.Empty, sut.Id);
        Assert.Equal(0, sut.Version);
    }
}
