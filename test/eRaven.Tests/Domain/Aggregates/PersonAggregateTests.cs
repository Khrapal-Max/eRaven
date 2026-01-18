//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonAggregateTests (updated)
//-----------------------------------------------------------------------------

using eRaven.Domain;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Enums;
using eRaven.Domain.Events.PersonEvents.Info;
using eRaven.Domain.Events.PersonEvents.Move;
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
    public void LoadFromHistory_when_created_should_initialize_state_and_set_stream_version()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: " Сержант ",
            Position: " Стрілець ",
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created)]);

        Assert.Equal(id, sut.Id);
        Assert.Equal(1, sut.Version);
        Assert.Equal(PersonLifecycle.Reserved, sut.Lifecycle);

        Assert.NotNull(sut.Personal);
        Assert.Equal("1234567890", sut.Personal!.Rnokpp);
        Assert.Equal("Ivanov", sut.Personal.LastName);
        Assert.Equal("Ivan", sut.Personal.FirstName);
        Assert.Equal("Ivanovich", sut.Personal.MiddleName);
        Assert.Equal("Ivanov Ivan Ivanovich", sut.Personal.FullName);

        Assert.Equal("Сержант", sut.Rank);
        Assert.Equal("Стрілець", sut.Position);
        Assert.Null(sut.BZVP);
        Assert.Null(sut.Weapon);
        Assert.Null(sut.Callsign);

        Assert.Null(sut.EnrolledAt);
        Assert.Null(sut.ExcludedAt);
        Assert.Null(sut.EnrollmentKind);
        Assert.Null(sut.EnrollmentReference);

        Assert.Empty(sut.GetUncommittedChanges());
    }

    [Fact]
    public void LoadFromHistory_when_event_voided_should_ignore_that_event_on_replay_but_keep_stream_version()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: null,
            Position: null,
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

        Assert.Equal(PersonLifecycle.Reserved, sut.Lifecycle);
        Assert.Null(sut.Rank);
        Assert.Equal(3, sut.Version);
    }

    [Fact]
    public void LoadFromHistory_void_of_void_should_restore_original_event_effect()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: null,
            Position: null,
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
    public void LoadFromHistory_when_enrolled_should_set_enrollment_fields_and_clear_excludedAt()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: "Сержант",
            Position: "P",
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var enrolled = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.AttachedByOrder,
            Reference: "№123",
            Reason: "Наказ",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "Сержант",
            Position: "Стрілець",
            PositionSort: 9999,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(2));

        var sut = new PersonAggregate();
        sut.LoadFromHistory([
            SE(1, created),
        SE(2, enrolled)
        ]);

        Assert.Equal(2, sut.Version);
        Assert.Equal(PersonLifecycle.Enrolled, sut.Lifecycle);
        Assert.Equal(new DateOnly(2026, 01, 10), sut.EnrolledAt);

        Assert.Equal(EnrollmentKind.AttachedByOrder, sut.EnrollmentKind);
        Assert.Equal("№123", sut.EnrollmentReference);

        Assert.Equal("Сержант", sut.Rank);
        Assert.Equal("Стрілець", sut.Position);
        Assert.Equal(9999, sut.PositionSort);

        Assert.Null(sut.ExcludedAt);
    }

    // =========================
    // commands
    // =========================

    [Fact]
    public void CreateReserved_should_produce_uncommitted_event_and_apply_state_immediately()
    {
        var id = Guid.NewGuid();

        var sut = PersonAggregate.CreateReserved(
            id: id,
            personal: Personal(),
            rank: " Сержант ",
            position: " Стрілець ",
            author: " tester ",
            nowUtc: NowUtc);

        Assert.Equal(id, sut.Id);
        Assert.Equal(PersonLifecycle.Reserved, sut.Lifecycle);

        Assert.Equal("Сержант", sut.Rank);
        Assert.Equal("Стрілець", sut.Position);

        var evt = Assert.IsType<PersonCreated>(Assert.Single(sut.GetUncommittedChanges()));
        Assert.Equal(id, evt.AggregateId);
        Assert.Equal("tester", evt.Author);

        Assert.Equal("Сержант", evt.Rank);
        Assert.Equal("Стрілець", evt.Position);
    }

    [Fact]
    public void ChangePosition_should_raise_event_and_trim_inputs()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: "Солдат",
            Position: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created)]);

        // positionSort <= 0
        Assert.Throws<ArgumentException>(() =>
            sut.ChangePosition(new DateOnly(2026, 01, 01), 0, "X", null, "tester", NowUtc.AddMinutes(1)));

        // author missing
        Assert.Throws<ArgumentException>(() =>
            sut.ChangePosition(new DateOnly(2026, 01, 01), 1, "X", null, "   ", NowUtc.AddMinutes(1)));

        sut.ChangePosition(new DateOnly(2026, 01, 01), 10, " Оператор ", null, " tester ", NowUtc.AddMinutes(1));

        Assert.Equal("Оператор", sut.Position);
        Assert.Equal(10, sut.PositionSort);

        var evt = Assert.IsType<PersonPositionChanged>(Assert.Single(sut.GetUncommittedChanges()));
        Assert.Equal("Оператор", evt.Position);
        Assert.Equal(10, evt.PositionSort);
        Assert.Equal("tester", evt.Author);
    }

    [Fact]
    public void Enroll_when_missing_rank_in_aggregate_should_throw()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: null,
            Position: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created)]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.Enroll(
                kind: EnrollmentKind.Unit,
                reference: null,
                reason: "ok",
                enrollDate: new DateOnly(2026, 01, 10),
                rank: string.Empty,
                positionSort: 1,
                position: "Стрілець",
                author: "tester",
                nowUtc: NowUtc.AddMinutes(1)));

        Assert.Contains("без звання", ex.Message);
    }

    [Fact]
    public void Enroll_when_missing_position_should_throw()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: "Солдат",
            Position: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created)]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.Enroll(
                kind: EnrollmentKind.Unit,
                reference: null,
                reason: "ok",
                enrollDate: new DateOnly(2026, 01, 10),
                rank: "Солдат",
                positionSort: 1,
                position: "   ",
                author: "tester",
                nowUtc: NowUtc.AddMinutes(1)));

        Assert.Contains("без посади", ex.Message);
    }

    [Fact]
    public void Enroll_when_ok_should_add_uncommitted_event_and_apply_state()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: "Сержант",
            Position: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created)]);

        sut.Enroll(
            kind: EnrollmentKind.AttachedByList,
            reference: " List-55 ",
            reason: "Test reason",
            enrollDate: new DateOnly(2026, 01, 10),
            rank: "Сержант",
            positionSort: 9999,
            position: " Оператор ",
            author: "tester",
            nowUtc: NowUtc.AddMinutes(1));

        Assert.Equal(PersonLifecycle.Enrolled, sut.Lifecycle);
        Assert.Equal(new DateOnly(2026, 01, 10), sut.EnrolledAt);
        Assert.Equal(EnrollmentKind.AttachedByList, sut.EnrollmentKind);
        Assert.Equal("List-55", sut.EnrollmentReference);

        Assert.Equal("Сержант", sut.Rank);
        Assert.Equal("Оператор", sut.Position);
        Assert.Equal(9999, sut.PositionSort);

        Assert.Null(sut.ExcludedAt);

        var evt = Assert.IsType<PersonEnrolled>(Assert.Single(sut.GetUncommittedChanges()));
        Assert.Equal(id, evt.AggregateId);
        Assert.Equal("Сержант", evt.Rank);
        Assert.Equal("Оператор", evt.Position);
        Assert.Equal(9999, evt.PositionSort);
        Assert.Equal("List-55", evt.Reference);
        Assert.Equal("tester", evt.Author);
    }

    [Fact]
    public void Exclude_should_add_uncommitted_event_and_apply_state_immediately_and_set_reserved()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: "Сержант",
            Position: "Оператор",
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var enrolled = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.Unit,
            Reference: null,
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 02),
            Rank: "Сержант",
            Position: "Оператор",
            PositionSort: 10,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created), SE(2, enrolled)]);

        sut.Exclude("reason", new DateOnly(2026, 01, 11), "tester", NowUtc.AddMinutes(2));

        Assert.Equal(PersonLifecycle.Reserved, sut.Lifecycle);
        Assert.Equal(new DateOnly(2026, 01, 11), sut.ExcludedAt);

        Assert.Equal("Сержант", sut.Rank);
        Assert.Equal("Оператор", sut.Position);
        Assert.Equal(10, sut.PositionSort);

        var evt = Assert.IsType<PersonExcluded>(Assert.Single(sut.GetUncommittedChanges()));
        Assert.Equal("reason", evt.Reason);
        Assert.Equal(new DateOnly(2026, 01, 11), evt.EffectiveDate);
    }

    [Fact]
    public void Exclude_when_not_enrolled_should_throw()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: "Сержант",
            Position: "Оператор",
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created)]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.Exclude("x", new DateOnly(2026, 01, 10), "tester", NowUtc.AddMinutes(1)));

        Assert.Contains("лише зараховану", ex.Message);
    }

    [Fact]
    public void Exclude_should_clear_enrollment_fields_but_keep_enrolledAt()
    {
        var id = Guid.NewGuid();
        var now = new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc);

        static PersonAggregate.StoredEvent SE(long version, IDomainEvent evt) => new(version, evt);

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: new PersonalInfo("1234567890", "Ivanov", "Ivan", null),
            Rank: "Солдат",
            Position: "Стрілець",
            Author: "t",
            OccurredAtUtc: now);

        var enrolled = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.Unit,
            Reference: "REF",
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "Солдат",
            PositionSort: 1,
            Position: "Оператор",
            Author: "t",
            OccurredAtUtc: now.AddMinutes(1));

        var excluded = new PersonExcluded(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Reason: "x",
            EffectiveDate: new DateOnly(2026, 01, 20),
            Author: "t",
            OccurredAtUtc: now.AddMinutes(2));

        var sut = new PersonAggregate();
        sut.LoadFromHistory(
        [
            SE(1, created),
            SE(2, enrolled),
            SE(3, excluded),
        ]);

        Assert.Equal(PersonLifecycle.Reserved, sut.Lifecycle);

        // ✅ cleared (це спрацює після твоєї правки Apply(PersonExcluded))
        Assert.Null(sut.EnrollmentKind);
        Assert.Null(sut.EnrollmentReference);

        // ✅ keep
        Assert.Equal(new DateOnly(2026, 01, 10), sut.EnrolledAt);

        Assert.Equal(new DateOnly(2026, 01, 20), sut.ExcludedAt);
        Assert.Equal(3, sut.Version);
    }

    [Fact]
    public void VoidEvent_should_add_uncommitted_change_and_validate_inputs()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: null,
            Position: null,
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

        var sut = PersonAggregate.CreateReserved(
            id: id,
            personal: Personal(),
            rank: null,
            position: null,
            author: "tester",
            nowUtc: NowUtc);

        Assert.Single(sut.GetUncommittedChanges());

        sut.LoadFromHistory([]);

        Assert.Empty(sut.GetUncommittedChanges());
        Assert.Equal(Guid.Empty, sut.Id);
        Assert.Equal(0, sut.Version);
    }

    [Fact]
    public void Enroll_after_excluded_should_be_allowed_and_clear_excludedAt()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: "Солдат",
            Position: "Стрілець",
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var enrolled1 = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.Unit,
            Reference: "A",
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 02),
            Rank: "Солдат",
            Position: "Стрілець",
            PositionSort: 10,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var excluded = new PersonExcluded(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Reason: "x",
            EffectiveDate: new DateOnly(2026, 01, 10),
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(2));

        var sut = new PersonAggregate();
        sut.LoadFromHistory([
            SE(1, created),
        SE(2, enrolled1),
        SE(3, excluded)
        ]);

        Assert.Equal(PersonLifecycle.Reserved, sut.Lifecycle);
        Assert.Equal(new DateOnly(2026, 01, 10), sut.ExcludedAt);

        sut.Enroll(
            kind: EnrollmentKind.AttachedByOrder,
            reference: "B",
            reason: "re-enroll",
            enrollDate: new DateOnly(2026, 06, 02),
            rank: "Солдат",
            positionSort: 9999,
            position: "Командир",
            author: "tester",
            nowUtc: NowUtc.AddMinutes(10));

        Assert.Equal(PersonLifecycle.Enrolled, sut.Lifecycle);
        Assert.Equal(new DateOnly(2026, 06, 02), sut.EnrolledAt);
        Assert.Null(sut.ExcludedAt);

        Assert.Equal(EnrollmentKind.AttachedByOrder, sut.EnrollmentKind);
        Assert.Equal("B", sut.EnrollmentReference);

        Assert.Equal("Солдат", sut.Rank);
        Assert.Equal("Командир", sut.Position);
        Assert.Equal(9999, sut.PositionSort);

        var evt = Assert.IsType<PersonEnrolled>(Assert.Single(sut.GetUncommittedChanges()));
        Assert.Equal(new DateOnly(2026, 06, 02), evt.EnrollDate);
        Assert.Equal("Командир", evt.Position);
        Assert.Equal(9999, evt.PositionSort);
        Assert.Equal("Солдат", evt.Rank);
    }

    [Fact]
    public void Enroll_after_excluded_with_date_not_after_excluded_should_throw()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: "Солдат",
            Position: "Стрілець",
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var excluded = new PersonExcluded(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Reason: "x",
            EffectiveDate: new DateOnly(2026, 01, 10),
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var sut = new PersonAggregate();
        sut.LoadFromHistory([SE(1, created), SE(2, excluded)]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.Enroll(
                kind: EnrollmentKind.Unit,
                reference: null,
                reason: "re-enroll",
                enrollDate: new DateOnly(2026, 01, 10),
                rank: "Солдат",
                positionSort: 1,
                position: "Стрілець",
                author: "tester",
                nowUtc: NowUtc.AddMinutes(2)));

        Assert.Contains("пізніше дати виключення", ex.Message);
    }
}
