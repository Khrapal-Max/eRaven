//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonTests (оновлено під поточну домен-модель)
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Events;
using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models;

public class PersonTests
{
    // ---------- FullName ----------

    [Fact]
    public void FullName_AllParts_ComposesCorrectly()
    {
        var person = new Person
        {
            LastName = "Шевченко",
            FirstName = "Тарас",
            MiddleName = "Григорович"
        };

        Assert.Equal("Шевченко Тарас Григорович", person.FullName);
    }

    [Fact]
    public void FullName_NoMiddleName_OmitsExtraSpace()
    {
        var person = new Person
        {
            LastName = "Шевченко",
            FirstName = "Тарас",
            MiddleName = null
        };

        Assert.Equal("Шевченко Тарас", person.FullName);
    }

    [Fact]
    public void FullName_OnlyLastName_ReturnsLastName()
    {
        var person = new Person
        {
            LastName = "Шевченко",
            FirstName = "",
            MiddleName = "   "
        };

        Assert.Equal("Шевченко", person.FullName);
    }

    [Fact]
    public void FullName_Ignores_Null_And_Whitespace_Parts()
    {
        var person = new Person
        {
            LastName = "Шевченко",
            FirstName = "   ",
            MiddleName = null
        };

        Assert.Equal("Шевченко", person.FullName);
    }

    [Fact]
    public void FullName_AllEmpty_ReturnsEmptyString()
    {
        var person = new Person
        {
            LastName = "",
            FirstName = "",
            MiddleName = null
        };

        Assert.Equal(string.Empty, person.FullName);
    }

    [Fact]
    public void FullName_Updates_When_Name_Parts_Change()
    {
        var p = new Person
        {
            LastName = "Іванов",
            FirstName = "Іван"
        };
        Assert.Equal("Іванов Іван", p.FullName);

        p.MiddleName = "Іванович";
        Assert.Equal("Іванов Іван Іванович", p.FullName);

        p.FirstName = "";
        Assert.Equal("Іванов Іванович", p.FullName);
    }

    // ---------- Defaults & Collections ----------

    [Fact]
    public void Collections_Are_Initialized_Empty()
    {
        var p = new Person();
        Assert.NotNull(p.StatusHistory);
        Assert.Empty(p.StatusHistory);

        Assert.NotNull(p.PositionAssignments);
        Assert.Empty(p.PositionAssignments);
    }

    [Fact]
    public void Default_Scalars_Are_Assigned_As_Declared()
    {
        var p = new Person();

        // string-и порожні за замовчуванням
        Assert.Equal(string.Empty, p.Rnokpp);
        Assert.Equal(string.Empty, p.LastName);
        Assert.Equal(string.Empty, p.FirstName);
        Assert.Equal(string.Empty, p.BZVP);

        // nullable — null
        Assert.Null(p.MiddleName);
        Assert.Null(p.Weapon);
        Assert.Null(p.Callsign);
        Assert.Null(p.PositionUnitId);
        Assert.Null(p.PositionUnit);
        Assert.Null(p.StatusKindId);
    }

    // ---------- Simple property set/get ----------

    [Fact]
    public void Can_Set_And_Get_All_Simple_Properties()
    {
        var pos = new PositionUnit
        {
            Id = Guid.NewGuid(),
            Code = "MECH-01",
            ShortName = "Механік",
            OrgPath = "Відділ В / Цех А"
        };
        var status = new StatusKind
        {
            Id = 3,
            Name = "Навчання",
            Code = "TRAIN",
            Order = 3,
            IsActive = true
        };

        var pid = pos.Id;

        var p = new Person
        {
            Id = Guid.NewGuid(),
            Rnokpp = "1234567890",
            Rank = "сержант",
            LastName = "Петренко",
            FirstName = "Петро",
            MiddleName = "Петрович",
            BZVP = "так",
            Weapon = "АК-74 №123",
            Callsign = "Сокіл",
            PositionUnitId = pid,
            PositionUnit = pos,
            StatusKindId = status.Id,
            StatusKind = status
        };

        Assert.Equal("1234567890", p.Rnokpp);
        Assert.Equal("сержант", p.Rank);
        Assert.Equal("Петренко", p.LastName);
        Assert.Equal("Петро", p.FirstName);
        Assert.Equal("Петрович", p.MiddleName);
        Assert.Equal("так", p.BZVP);
        Assert.Equal("АК-74 №123", p.Weapon);
        Assert.Equal("Сокіл", p.Callsign);

        Assert.Equal(pid, p.PositionUnitId);
        Assert.Same(pos, p.PositionUnit);

        Assert.Equal(status.Id, p.StatusKindId);
        Assert.Same(status, p.StatusKind);

        Assert.Equal("Петренко Петро Петрович", p.FullName);
    }

    // ---------- PositionUnit link semantics ----------

    [Fact]
    public void PositionUnit_Link_Is_Consistent_With_Id()
    {
        var unitId = Guid.NewGuid();
        var unit = new PositionUnit
        {
            Id = unitId,
            ShortName = "Сапер",
            OrgPath = "Рота 1 / Взвод 2"
        };

        var p = new Person
        {
            PositionUnitId = unitId,
            PositionUnit = unit
        };

        Assert.Equal(unitId, p.PositionUnitId);
        Assert.Same(unit, p.PositionUnit);

        // Перевірка зручного FullName у PositionUnit (якщо він є у домені)
        Assert.Equal("Сапер Рота 1 / Взвод 2", unit.FullName);
    }

    // ---------- Агрегатна поведінка ----------

    [Fact]
    public void Create_RaisesPersonCreatedEvent()
    {
        var id = Guid.NewGuid();
        var createdAt = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var person = Person.Create(
            id,
            "1234567890",
            "капітан",
            "Іваненко",
            "Іван",
            "Іванович",
            "так",
            "АК-74",
            "Сокіл",
            false,
            null,
            createdAt);

        var @event = Assert.Single(person.PendingEvents);
        var created = Assert.IsType<PersonCreatedEvent>(@event);
        Assert.Equal(id, created.PersonId);
        Assert.Equal(createdAt, created.OccurredAtUtc);
        Assert.Equal("Іваненко", person.LastName);
        Assert.Equal(createdAt, person.CreatedUtc);
        Assert.Equal(createdAt, person.ModifiedUtc);
    }

    [Fact]
    public void PendingEvents_CanBeCleared()
    {
        var person = Person.Create(
            Guid.NewGuid(),
            "1111111111",
            "солдат",
            "Петренко",
            "Петро",
            null,
            "", null, null, false, null,
            DateTime.UtcNow);

        Assert.NotEmpty(person.PendingEvents);
        person.ClearPendingEvents();
        Assert.Empty(person.PendingEvents);
    }

    [Fact]
    public void AssignToPosition_CreatesActiveSnapshot()
    {
        var person = new Person { Id = Guid.NewGuid() };
        var unit = new PositionUnit { Id = Guid.NewGuid(), ShortName = "Оператор", OrgPath = "Рота А" };
        var assignedAt = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc);

        person.AssignToPosition(unit, assignedAt, "початок", "tester");

        var snapshot = Assert.Single(person.PositionAssignments);
        Assert.Equal(unit.Id, snapshot.PositionUnitId);
        Assert.True(snapshot.IsActive);
        Assert.Equal(assignedAt, snapshot.OpenUtc);
        Assert.Equal("початок", snapshot.Note);
        Assert.Equal("tester", snapshot.Author);
        Assert.Equal(unit.Id, person.PositionUnitId);
        Assert.Same(unit, person.PositionUnit);
        Assert.Equal(assignedAt, person.ModifiedUtc);

        var evt = Assert.IsType<PersonPositionAssignedEvent>(Assert.Single(person.PendingEvents));
        Assert.Equal(snapshot.Id, evt.AssignmentId);
    }

    [Fact]
    public void AssignToPosition_Reassign_ClosesPrevious()
    {
        var person = new Person { Id = Guid.NewGuid() };
        var unitA = new PositionUnit { Id = Guid.NewGuid(), ShortName = "Оператор", OrgPath = "Рота А" };
        var unitB = new PositionUnit { Id = Guid.NewGuid(), ShortName = "Механік", OrgPath = "Рота Б" };

        var t1 = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2025, 1, 15, 9, 30, 0, DateTimeKind.Utc);

        person.AssignToPosition(unitA, t1, "первинне", "tester");
        person.AssignToPosition(unitB, t2, "переведення", "hr");

        Assert.Equal(unitB.Id, person.PositionUnitId);
        Assert.Equal(2, person.PositionAssignments.Count);

        var closed = person.PositionAssignments.Single(x => x.PositionUnitId == unitA.Id);
        Assert.False(closed.IsActive);
        Assert.Equal(t2, closed.CloseUtc);

        var active = person.CurrentAssignment;
        Assert.NotNull(active);
        Assert.Equal(unitB.Id, active!.PositionUnitId);
        Assert.True(active.IsActive);
        Assert.Equal(t2, person.ModifiedUtc);

        Assert.Collection(person.PendingEvents,
            e => Assert.IsType<PersonPositionAssignedEvent>(e),
            e => Assert.IsType<PersonPositionRemovedEvent>(e),
            e => Assert.IsType<PersonPositionAssignedEvent>(e));
    }

    [Fact]
    public void RemoveFromPosition_DeactivatesSnapshot()
    {
        var person = new Person { Id = Guid.NewGuid() };
        var unit = new PositionUnit { Id = Guid.NewGuid(), ShortName = "Радист", OrgPath = "Рота 3" };
        var start = new DateTime(2025, 2, 1, 7, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(12);

        person.AssignToPosition(unit, start, null, null);
        person.RemoveFromPosition(end, "знято", "cmdr");

        var snapshot = Assert.Single(person.PositionAssignments);
        Assert.False(snapshot.IsActive);
        Assert.Equal(end, snapshot.CloseUtc);
        Assert.Null(person.PositionUnitId);
        Assert.Null(person.PositionUnit);
        Assert.Equal(end, person.ModifiedUtc);

        Assert.Collection(person.PendingEvents,
            e => Assert.IsType<PersonPositionAssignedEvent>(e),
            e => Assert.IsType<PersonPositionRemovedEvent>(e));
    }

    [Fact]
    public void RemoveFromPosition_WithoutActive_Throws()
    {
        var person = new Person { Id = Guid.NewGuid() };

        Assert.Throws<InvalidOperationException>(() =>
            person.RemoveFromPosition(DateTime.UtcNow, null, null));
    }

    [Fact]
    public void SetStatus_AddsSnapshots_AndRespectsTransitions()
    {
        var person = new Person { Id = Guid.NewGuid() };
        var kindA = new StatusKind { Id = 1, Name = "В строю", Code = "ACTIVE", Order = 1 };
        var kindB = new StatusKind { Id = 2, Name = "У відпустці", Code = "LEAVE", Order = 2 };

        var t1 = new DateTime(2025, 3, 1, 6, 0, 0, DateTimeKind.Utc);
        var t2 = t1.AddHours(6);

        person.SetStatus(kindA, t1, "вийшов на службу", "cmdr", null);
        person.SetStatus(kindB, t2, "наказ", "hr", new[]
        {
            new StatusTransition { FromStatusKindId = kindA.Id, ToStatusKindId = kindB.Id }
        });

        Assert.Equal(kindB.Id, person.StatusKindId);
        Assert.Same(kindB, person.StatusKind);
        Assert.Equal(t2, person.ModifiedUtc);
        Assert.Equal(2, person.StatusHistory.Count);

        var first = person.StatusHistory.First(s => s.StatusKindId == kindA.Id);
        Assert.False(first.IsActive);
        Assert.Equal((short)1, first.Sequence);
        Assert.Equal(t2, first.Modified);

        var second = person.StatusHistory.First(s => s.StatusKindId == kindB.Id);
        Assert.True(second.IsActive);
        Assert.Equal((short)2, second.Sequence);
        Assert.Equal("наказ", second.Note);
    }

    [Fact]
    public void SetStatus_Disallows_Transition_IfNotConfigured()
    {
        var person = new Person { Id = Guid.NewGuid() };
        var kindA = new StatusKind { Id = 1, Name = "В строю", Code = "ACTIVE", Order = 1 };
        var kindC = new StatusKind { Id = 3, Name = "У наряді", Code = "DUTY", Order = 3 };

        person.SetStatus(kindA, new DateTime(2025, 4, 1, 6, 0, 0, DateTimeKind.Utc), null, null, null);

        Assert.Throws<InvalidOperationException>(() =>
            person.SetStatus(kindC,
                new DateTime(2025, 4, 1, 10, 0, 0, DateTimeKind.Utc),
                null,
                null,
                Array.Empty<StatusTransition>()));
    }

    [Fact]
    public void SetStatus_SameStatus_UpdatesNoteOnly()
    {
        var person = new Person { Id = Guid.NewGuid() };
        var kind = new StatusKind { Id = 1, Name = "В строю", Code = "ACTIVE", Order = 1 };

        var t1 = new DateTime(2025, 5, 1, 6, 0, 0, DateTimeKind.Utc);
        var t2 = t1.AddHours(2);

        person.SetStatus(kind, t1, "первинний", "cmdr", null);
        person.SetStatus(kind, t2, "оновлений", "cmdr", null);

        var status = Assert.Single(person.StatusHistory);
        Assert.True(status.IsActive);
        Assert.Equal("оновлений", status.Note);
        Assert.Equal(t2, status.Modified);
        Assert.Equal(t2, person.ModifiedUtc);

        Assert.Collection(person.PendingEvents,
            e => Assert.IsType<PersonStatusSetEvent>(e),
            e => Assert.IsType<PersonStatusNoteUpdatedEvent>(e));
    }

    [Fact]
    public void SetStatusActiveState_TogglesAndUpdatesPerson()
    {
        var person = new Person { Id = Guid.NewGuid() };
        var kindA = new StatusKind { Id = 1, Name = "В строю", Code = "ACTIVE", Order = 1 };
        var kindB = new StatusKind { Id = 2, Name = "Відпустка", Code = "LEAVE", Order = 2 };

        var openedA = new DateTime(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var openedB = openedA.AddHours(4);

        person.SetStatus(kindA, openedA, null, null, null);
        person.SetStatus(kindB, openedB, null, null, null);

        var current = person.CurrentStatus!;
        Assert.Equal(kindB.Id, current.StatusKindId);

        var changed = person.SetStatusActiveState(current.Id, shouldBeActive: false, openedB.AddHours(1));

        Assert.True(changed);
        Assert.False(current.IsActive);
        Assert.Equal(kindA.Id, person.StatusKindId);
        Assert.Same(kindA, person.StatusKind);
    }

    [Fact]
    public void SetStatusActiveState_ReactivatesAndAdjustsSequence()
    {
        var person = new Person { Id = Guid.NewGuid() };
        var kindA = new StatusKind { Id = 1, Name = "В строю", Code = "ACTIVE", Order = 1 };
        var kindB = new StatusKind { Id = 2, Name = "Відрядження", Code = "DISP", Order = 2 };

        var opened = new DateTime(2025, 6, 10, 8, 0, 0, DateTimeKind.Utc);

        var first = person.SetStatus(kindA, opened, null, null, null);
        var second = person.SetStatus(kindB, opened.AddHours(1), null, null, null);

        person.SetStatusActiveState(second.Id, shouldBeActive: false, opened.AddHours(2));

        var changed = person.SetStatusActiveState(second.Id, shouldBeActive: true, opened.AddHours(3));

        Assert.True(changed);
        Assert.True(second.IsActive);
        Assert.True(second.Sequence > first.Sequence);
        Assert.Equal(kindB.Id, person.StatusKindId);
    }

    [Fact]
    public void BuildFromHistory_RecreatesState()
    {
        var personId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();

        var events = new IPersonEvent[]
        {
            new PersonCreatedEvent(personId, "123", "лейтенант", "Сидоренко", "Сидір", null, "", null, null, false, null, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            new PersonPositionAssignedEvent(personId, assignmentId, unitId, new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc), "", null),
            new PersonStatusSetEvent(personId, statusId, 5, new DateTime(2025, 1, 3, 0, 0, 0, DateTimeKind.Utc), 1, null, null, null, null)
        };

        var rebuilt = Person.BuildFromHistory(personId, events);

        Assert.Equal(unitId, rebuilt.PositionUnitId);
        Assert.Equal(5, rebuilt.StatusKindId);
        Assert.Equal(1, rebuilt.StatusHistory.Count);
        Assert.Equal(assignmentId, rebuilt.CurrentAssignment!.Id);
        Assert.Equal(statusId, rebuilt.CurrentStatus!.Id);
        Assert.Empty(rebuilt.PendingEvents);
    }

    [Fact]
    public void Projections_ReturnCurrentSnapshots()
    {
        var person = Person.Create(
            Guid.NewGuid(),
            "2222222222",
            "майор",
            "Коваленко",
            "Кирило",
            null,
            "",
            null,
            null,
            false,
            null,
            new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        person.ClearPendingEvents();

        var unit = new PositionUnit { Id = Guid.NewGuid(), ShortName = "Командир", OrgPath = "Батальйон" };
        var status = new StatusKind { Id = 7, Name = "На завданні", Code = "TASK", Order = 7 };

        var assignedAt = new DateTime(2025, 2, 2, 0, 0, 0, DateTimeKind.Utc);
        var statusAt = new DateTime(2025, 2, 3, 0, 0, 0, DateTimeKind.Utc);

        person.AssignToPosition(unit, assignedAt, "", "officer");
        person.SetStatus(status, statusAt, "", "officer", null);

        var plan = PlanAction.Create(
            person.Id,
            "Вихід",
            statusAt,
            status.Id,
            null,
            ActionState.PlanAction,
            MoveType.Dispatch,
            "База",
            "Група",
            "Екіпаж",
            "",
            person.Rnokpp,
            person.FullName,
            person.Rank,
            unit.ShortName,
            person.BZVP,
            person.Weapon ?? string.Empty,
            person.Callsign ?? string.Empty,
            status.Name);

        person.AddPlanAction(plan);

        var card = person.ToCardProjection();
        Assert.Equal(person.Id, card.PersonId);
        Assert.Equal(unit.Id, card.PositionUnitId);
        Assert.Equal(status.Id, card.StatusKindId);

        var assignments = person.BuildAssignmentsProjection();
        var statuses = person.BuildStatusProjection();
        var plans = person.BuildPlanActionsProjection();

        Assert.Single(assignments);
        Assert.Single(statuses);
        Assert.Single(plans);
        Assert.Equal(plan.PlanActionName, plans[0].PlanActionName);
    }
}
