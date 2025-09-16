//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Order
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models;

public class OrderActionTests
{
    [Fact]
    public void NewOrderAction_Defaults_AreNeutral()
    {
        // Arrange
        // Act
        var oa = new OrderAction();

        // Assert
        Assert.Equal(Guid.Empty, oa.Id);
        Assert.Equal(Guid.Empty, oa.OrderId);
        Assert.Equal(Guid.Empty, oa.PlanId);
        Assert.Equal(Guid.Empty, oa.PlanActionId);
        Assert.Equal(Guid.Empty, oa.PersonId);

        Assert.Null(oa.Order);
        Assert.Null(oa.Person);

        Assert.Equal(default, oa.ActionType);
        Assert.Equal(default, oa.EventAtUtc);
        Assert.Equal(DateTimeKind.Unspecified, oa.EventAtUtc.Kind);

        // strings are null due to default!
        Assert.Null(oa.Location);
        Assert.Null(oa.GroupName);
        Assert.Null(oa.CrewName);

        Assert.Null(oa.Rnokpp);
        Assert.Null(oa.FullName);
        Assert.Null(oa.RankName);
        Assert.Null(oa.PositionName);
        Assert.Null(oa.BZVP);
        Assert.Null(oa.Weapon);
        Assert.Null(oa.Callsign);
        Assert.Null(oa.StatusKindOnDate);
    }

    [Fact]
    public void CanAssign_CoreProps_And_Snapshot()
    {
        // Arrange
        var order = new Order { Id = Guid.NewGuid(), Name = "НК-2025/030", EffectiveMomentUtc = DateTime.UtcNow };
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
        var planId = Guid.NewGuid();
        var planActionId = Guid.NewGuid();
        var eventUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-1), DateTimeKind.Utc);

        // Act
        var oa = new OrderAction
        {
            Id = Guid.NewGuid(),

            OrderId = order.Id,
            Order = order,

            PlanId = planId,
            PlanActionId = planActionId,

            PersonId = person.Id,
            Person = person,

            ActionType = PlanActionType.Dispatch,
            EventAtUtc = eventUtc,
            Location = "Карʼєр-1",
            GroupName = "Бригада-2",
            CrewName = "Зміна-А",

            // snapshot (копія в наказі)
            Rnokpp = person.Rnokpp,
            FullName = person.FullName,
            RankName = person.Rank,
            PositionName = "Командир відділення",
            BZVP = person.BZVP,
            Weapon = person.Weapon ?? "",
            Callsign = person.Callsign ?? "",
            StatusKindOnDate = "В районі"
        };

        // Assert
        Assert.Same(order, oa.Order);
        Assert.Equal(order.Id, oa.OrderId);
        Assert.Same(person, oa.Person);
        Assert.Equal(person.Id, oa.PersonId);
        Assert.Equal(planId, oa.PlanId);
        Assert.Equal(planActionId, oa.PlanActionId);

        Assert.Equal(PlanActionType.Dispatch, oa.ActionType);
        Assert.Equal(DateTimeKind.Utc, oa.EventAtUtc.Kind);
        Assert.Equal(eventUtc, oa.EventAtUtc);

        Assert.Equal("Карʼєр-1", oa.Location);
        Assert.Equal("Бригада-2", oa.GroupName);
        Assert.Equal("Зміна-А", oa.CrewName);

        Assert.Equal("1234567890", oa.Rnokpp);
        Assert.Equal("Петренко Іван", oa.FullName);
        Assert.Equal("Сержант", oa.RankName);
        Assert.Equal("Командир відділення", oa.PositionName);
        Assert.Equal("Так", oa.BZVP);
        Assert.Equal("АК-74", oa.Weapon);
        Assert.Equal("Сокіл", oa.Callsign);
        Assert.Equal("В районі", oa.StatusKindOnDate);
    }

    [Fact]
    public void EventAtUtc_ShouldBeUtc()
    {
        // Arrange
        var oa = new OrderAction
        {
            // Act
            EventAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
        };

        // Assert
        Assert.Equal(DateTimeKind.Utc, oa.EventAtUtc.Kind);
    }

    [Fact]
    public void Adding_To_Order_Actions_Collection_Works()
    {
        // Arrange
        var order = new Order { Id = Guid.NewGuid(), Name = "НК-2025/031", EffectiveMomentUtc = DateTime.UtcNow };
        var person = new Person { Id = Guid.NewGuid(), LastName = "Івашенко", FirstName = "Павло", Rnokpp = "1111111111", BZVP = "Ні" };

        var oa = new OrderAction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            PlanId = Guid.NewGuid(),
            PlanActionId = Guid.NewGuid(),
            PersonId = person.Id,
            Person = person,
            ActionType = PlanActionType.Return,
            EventAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Location = "База",
            GroupName = "Бригада-1",
            CrewName = "—",
            Rnokpp = person.Rnokpp,
            FullName = person.FullName,
            RankName = "Солдат",
            PositionName = "Командир відділення",
            BZVP = person.BZVP,
            Weapon = "",
            Callsign = "",
            StatusKindOnDate = "Повернувся"
        };

        // Act
        order.Actions.Add(oa);

        // Assert
        Assert.Single(order.Actions);
        Assert.Same(order, order.Actions.First().Order);
        Assert.Equal(order.Id, order.Actions.First().OrderId);
    }

    [Fact]
    public void Snapshot_IsIndependent_From_Person_LaterChanges()
    {
        // Arrange
        var order = new Order { Id = Guid.NewGuid(), Name = "НК-2025/032", EffectiveMomentUtc = DateTime.UtcNow };
        var person = new Person
        {
            Id = Guid.NewGuid(),
            Rnokpp = "2222222222",
            Rank = "Солдат",
            LastName = "Бондаренко",
            FirstName = "Олег",
            BZVP = "Так",
            Weapon = "ПКМ",
            Callsign = "Вітер"
        };

        var oa = new OrderAction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            PlanId = Guid.NewGuid(),
            PlanActionId = Guid.NewGuid(),
            PersonId = person.Id,
            Person = person,

            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Location = "Карʼєр-2",
            GroupName = "Бригада-3",
            CrewName = "Зміна-B",

            Rnokpp = person.Rnokpp,
            FullName = person.FullName,
            RankName = person.Rank,
            PositionName = "Командир відділення",
            BZVP = person.BZVP,
            Weapon = person.Weapon ?? "",
            Callsign = person.Callsign ?? "",
            StatusKindOnDate = "Виїхав"
        };

        // Act — змінюємо Person після формування snapshot
        person.Rank = "Сержант";
        person.Weapon = "АК-74";
        person.Callsign = "Грім";

        // Assert — snapshot у наказі не змінюється
        Assert.Equal("Солдат", oa.RankName);
        Assert.Equal("ПКМ", oa.Weapon);
        Assert.Equal("Вітер", oa.Callsign);
    }
}
