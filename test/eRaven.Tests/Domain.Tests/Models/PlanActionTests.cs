//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActionTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models;

public class PlanActionTests
{
    [Fact]
    public void NewPlanAction_Defaults_AreNeutral()
    {
        // Arrange
        // Act
        var a = new PlanAction();

        // Assert
        Assert.Equal(Guid.Empty, a.Id);
        Assert.Equal(Guid.Empty, a.PlanId);
        Assert.Equal(Guid.Empty, a.PersonId);

        Assert.Null(a.Plan);
        Assert.Null(a.Person);

        Assert.Equal(default, a.ActionType);
        Assert.Equal(default, a.EventAtUtc); // 0001-01-01, Kind = Unspecified

        // snapshot fields are null by default (default!)
        Assert.Null(a.Rnokpp);
        Assert.Null(a.FullName);
        Assert.Null(a.RankName);
        Assert.Null(a.PositionName);
        Assert.Null(a.BZVP);
        Assert.Null(a.Weapon);
        Assert.Null(a.Callsign);
        Assert.Null(a.StatusKindOnDate);
    }

    [Fact]
    public void CanAssign_CoreProps_And_Snapshot()
    {
        // Arrange
        var plan = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-100" };
        var person = new Person
        {
            Id = Guid.NewGuid(),
            Rnokpp = "1234567890",
            Rank = "Сержант",
            LastName = "Петренко",
            FirstName = "Іван",
            BZVP = "Так",
            Weapon = "АК-74",
            Callsign = "Сокіл"
        };

        // Act
        var a = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Plan = plan,
            PersonId = person.Id,
            Person = person,
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Location = "Карʼєр-1",
            GroupName = "Бригада-2",
            CrewName = "Зміна-А",

            Rnokpp = person.Rnokpp,
            FullName = person.FullName,
            RankName = person.Rank,
            PositionName = person.PositionUnit?.ShortName ?? "Механік",
            BZVP = person.BZVP,
            Weapon = person.Weapon ?? "",
            Callsign = person.Callsign ?? "",
            StatusKindOnDate = "В районі"
        };

        // Assert
        Assert.Equal(plan.Id, a.PlanId);
        Assert.Same(plan, a.Plan);
        Assert.Equal(person.Id, a.PersonId);
        Assert.Same(person, a.Person);

        Assert.Equal(PlanActionType.Dispatch, a.ActionType);
        Assert.Equal(DateTimeKind.Utc, a.EventAtUtc.Kind);

        Assert.Equal("1234567890", a.Rnokpp);
        Assert.Equal("Петренко Іван", a.FullName);
        Assert.Equal("Сержант", a.RankName);
        Assert.Equal("Механік", a.PositionName);
        Assert.Equal("Так", a.BZVP);
        Assert.Equal("АК-74", a.Weapon);
        Assert.Equal("Сокіл", a.Callsign);
        Assert.Equal("В районі", a.StatusKindOnDate);
    }

    [Fact]
    public void EventAtUtc_ShouldBeUtc()
    {
        // Arrange
        var a = new PlanAction
        {
            // Act
            EventAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
        };

        // Assert
        Assert.Equal(DateTimeKind.Utc, a.EventAtUtc.Kind);
    }

    [Fact]
    public void Link_With_Plan_Collection_IsConsistent()
    {
        // Arrange
        var plan = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-200" };
        var personId = Guid.NewGuid();
        var a = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Plan = plan,
            PersonId = personId,
            Person = new Person { Id = personId, LastName = "A", FirstName = "B" },
            ActionType = PlanActionType.Return,
            EventAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Location = "L",
            GroupName = "G",
            CrewName = "C",
            Rnokpp = "1111111111",
            FullName = "A B",
            RankName = "R",
            PositionName = "P",
            BZVP = "Y",
            Weapon = "W",
            Callsign = "CS",
            StatusKindOnDate = "Some"
        };

        // Act
        plan.PlanActions.Add(a);

        // Assert
        Assert.Single(plan.PlanActions);
        Assert.Same(a, plan.PlanActions.First());
        Assert.Same(plan, a.Plan);
        Assert.Equal(plan.Id, a.PlanId);
    }

    [Fact]
    public void LastPlanState_ByLatestEventAtUtc_Works()
    {
        // Arrange
        var plan = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-300" };
        var pid = Guid.NewGuid();
        var person = new Person { Id = pid, LastName = "X", FirstName = "Y", Rnokpp = "2222222222", BZVP = "Y" };

        var t1 = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-2), DateTimeKind.Utc);
        var t2 = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-1), DateTimeKind.Utc);

        var a1 = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Plan = plan,
            PersonId = pid,
            Person = person,
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = t1,
            Location = "L1",
            GroupName = "G1",
            CrewName = "C1",
            Rnokpp = "2222222222",
            FullName = "X Y",
            RankName = "R",
            PositionName = "P",
            BZVP = "Y",
            Weapon = "",
            Callsign = "",
            StatusKindOnDate = "S1"
        };
        var a2 = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Plan = plan,
            PersonId = pid,
            Person = person,
            ActionType = PlanActionType.Return,
            EventAtUtc = t2,
            Location = "L2",
            GroupName = "G2",
            CrewName = "C2",
            Rnokpp = "2222222222",
            FullName = "X Y",
            RankName = "R",
            PositionName = "P",
            BZVP = "Y",
            Weapon = "",
            Callsign = "",
            StatusKindOnDate = "S2"
        };

        plan.PlanActions.Add(a1);
        plan.PlanActions.Add(a2);

        // Act
        var last = plan.PlanActions
            .Where(x => x.PersonId == pid)
            .OrderByDescending(x => x.EventAtUtc)
            .First();

        // Assert
        Assert.Same(a2, last);
        Assert.Equal(PlanActionType.Return, last.ActionType);
        Assert.True(last.EventAtUtc > a1.EventAtUtc);
    }

    [Fact]
    public void Snapshot_IsIndependent_From_Person_LaterChanges()
    {
        // Arrange
        var person = new Person
        {
            Id = Guid.NewGuid(),
            Rnokpp = "3333333333",
            Rank = "Солдат",
            LastName = "Івашенко",
            FirstName = "Павло",
            BZVP = "Ні",
            Weapon = "ПКМ",
            Callsign = "Вітер"
        };

        var a = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            PersonId = person.Id,
            Person = person,
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Location = "L",
            GroupName = "G",
            CrewName = "C",

            // take snapshot NOW
            Rnokpp = person.Rnokpp,
            FullName = person.FullName,
            RankName = person.Rank,
            PositionName = "Pos",
            BZVP = person.BZVP,
            Weapon = person.Weapon ?? "",
            Callsign = person.Callsign ?? "",
            StatusKindOnDate = "S"
        };

        // Act (later changes to person)
        person.Rank = "Сержант";
        person.Weapon = "АК-74";
        person.Callsign = "Грім";

        // Assert (snapshot unchanged)
        Assert.Equal("3333333333", a.Rnokpp);
        Assert.Equal("Івашенко Павло", a.FullName);
        Assert.Equal("Солдат", a.RankName);
        Assert.Equal("ПКМ", a.Weapon);
        Assert.Equal("Вітер", a.Callsign);
    }
}
