//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// OrderTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models;

public class OrderTests
{
    [Fact]
    public void NewOrder_Defaults_AreCorrect()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var order = new Order();
        var after = DateTime.UtcNow;

        // Assert
        Assert.Equal(Guid.Empty, order.Id);
        Assert.Null(order.Name);                       // default!
        Assert.Null(order.Author);

        // EffectiveMomentUtc не заданий (default Struct)
        Assert.Equal(default, order.EffectiveMomentUtc);
        Assert.Equal(DateTimeKind.Unspecified, order.EffectiveMomentUtc.Kind);

        // Колекції створені
        Assert.NotNull(order.Plans);
        Assert.Empty(order.Plans);
        Assert.NotNull(order.Actions);
        Assert.Empty(order.Actions);

        // RecordedUtc виставляється конструктором властивості
        Assert.Equal(DateTimeKind.Utc, order.RecordedUtc.Kind);
        Assert.True(order.RecordedUtc >= before && order.RecordedUtc <= after);
    }

    [Fact]
    public void CanSet_Name_Author_And_EffectiveMomentUtcUtc()
    {
        // Arrange
        var order = new Order
        {
            // Act
            Name = "НК-2025/015",
            Author = "duty.officer",
            EffectiveMomentUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
        };

        // Assert
        Assert.Equal("НК-2025/015", order.Name);
        Assert.Equal("duty.officer", order.Author);
        Assert.Equal(DateTimeKind.Utc, order.EffectiveMomentUtc.Kind);
    }

    [Fact]
    public void Order_CanHold_Many_Plans()
    {
        // Arrange
        var order = new Order { Id = Guid.NewGuid(), Name = "НК-2025/016", EffectiveMomentUtc = DateTime.UtcNow };
        var p1 = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-101" };
        var p2 = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-102" };

        // Act
        // Встановлюємо обидві сторони вручну (POCO, без EF fixup)
        p1.Order = order; p1.OrderId = order.Id;
        p2.Order = order; p2.OrderId = order.Id;
        order.Plans.Add(p1);
        order.Plans.Add(p2);

        // Assert
        Assert.Equal(2, order.Plans.Count);
        Assert.Same(order, p1.Order);
        Assert.Same(order, p2.Order);
        Assert.Equal(order.Id, p1.OrderId);
        Assert.Equal(order.Id, p2.OrderId);
    }

    [Fact]
    public void Orders_Have_Independent_Plan_Collections()
    {
        // Arrange
        var o1 = new Order { Id = Guid.NewGuid(), Name = "НК-2025/017", EffectiveMomentUtc = DateTime.UtcNow };
        var o2 = new Order { Id = Guid.NewGuid(), Name = "НК-2025/018", EffectiveMomentUtc = DateTime.UtcNow };

        var p = new Plan
        {
            Id = Guid.NewGuid(),
            PlanNumber = "PL-103",         // Act
            Order = o1,
            OrderId = o1.Id
        };
        o1.Plans.Add(p);

        // Assert
        Assert.Single(o1.Plans);
        Assert.Empty(o2.Plans);
        Assert.Same(o1, p.Order);
    }

    [Fact]
    public void Order_CanHold_Actions_WithSnapshot()
    {
        // Arrange
        var order = new Order { Id = Guid.NewGuid(), Name = "НК-2025/019", EffectiveMomentUtc = DateTime.UtcNow };
        var plan = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-200" };

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

        var planAction = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Plan = plan,
            PersonId = person.Id,
            Person = person,
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-1), DateTimeKind.Utc),
            Location = "Карʼєр-1",
            GroupName = "Бригада-2",
            CrewName = "Зміна-А",
            Rnokpp = person.Rnokpp,
            FullName = person.FullName,
            RankName = person.Rank,
            PositionName = "Командир відділення",
            BZVP = person.BZVP,
            Weapon = person.Weapon ?? "",
            Callsign = person.Callsign ?? "",
            StatusKindOnDate = "В районі"
        };

        // Act
        var oa = new OrderAction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            PlanId = plan.Id,
            PlanActionId = planAction.Id,
            PersonId = person.Id,
            Person = person,

            ActionType = planAction.ActionType,
            EventAtUtc = planAction.EventAtUtc,
            Location = planAction.Location,
            GroupName = planAction.GroupName,
            CrewName = planAction.CrewName,

            // snapshot copy
            Rnokpp = planAction.Rnokpp,
            FullName = planAction.FullName,
            RankName = planAction.RankName,
            PositionName = planAction.PositionName,
            BZVP = planAction.BZVP,
            Weapon = planAction.Weapon,
            Callsign = planAction.Callsign,
            StatusKindOnDate = planAction.StatusKindOnDate
        };

        order.Actions.Add(oa);

        // Assert
        Assert.Single(order.Actions);
        var first = order.Actions.First();
        Assert.Same(order, first.Order);
        Assert.Equal(order.Id, first.OrderId);
        Assert.Equal(plan.Id, first.PlanId);
        Assert.Equal(planAction.Id, first.PlanActionId);
        Assert.Equal(person.Id, first.PersonId);
        Assert.Equal(PlanActionType.Dispatch, first.ActionType);
        Assert.Equal(DateTimeKind.Utc, first.EventAtUtc.Kind);

        Assert.Equal("1234567890", first.Rnokpp);
        Assert.Equal("Петренко Іван", first.FullName);
        Assert.Equal("Сержант", first.RankName);
        Assert.Equal("Командир відділення", first.PositionName);
        Assert.Equal("Так", first.BZVP);
        Assert.Equal("АК-74", first.Weapon);
        Assert.Equal("Сокіл", first.Callsign);
        Assert.Equal("В районі", first.StatusKindOnDate);
    }

    [Fact]
    public void OrderAction_Snapshot_IsIndependent_From_Person_LaterChanges()
    {
        // Arrange
        var order = new Order { Id = Guid.NewGuid(), Name = "НК-2025/020", EffectiveMomentUtc = DateTime.UtcNow };
        var person = new Person
        {
            Id = Guid.NewGuid(),
            Rnokpp = "1111111111",
            Rank = "Солдат",
            LastName = "Івашенко",
            FirstName = "Павло",
            BZVP = "Ні",
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

            ActionType = PlanActionType.Return,
            EventAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Location = "База",
            GroupName = "Бригада-1",
            CrewName = "—",

            Rnokpp = person.Rnokpp,
            FullName = person.FullName,
            RankName = person.Rank,
            PositionName = "Командир відділення",
            BZVP = person.BZVP,
            Weapon = person.Weapon ?? "",
            Callsign = person.Callsign ?? "",
            StatusKindOnDate = "Повернувся"
        };

        order.Actions.Add(oa);

        // Act (зміни у Person після формування рядка наказу)
        person.Rank = "Сержант";
        person.Weapon = "АК-74";
        person.Callsign = "Грім";

        // Assert — snapshot у наказі НЕ змінюється
        var first = order.Actions.First();
        Assert.Equal("Солдат", first.RankName);
        Assert.Equal("ПКМ", first.Weapon);
        Assert.Equal("Вітер", first.Callsign);
    }
}
