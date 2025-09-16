//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanTests (final for the minimal Plan model)
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models;

public class PlanTests
{
    [Fact]
    public void NewPlan_Defaults_AreCorrect()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var plan = new Plan();
        var after = DateTime.UtcNow;

        // Assert
        Assert.Equal(Guid.Empty, plan.Id);                 // POCO без конструктора — Guid.Empty
        Assert.Null(plan.PlanNumber);                      // default!
        Assert.Equal(PlanState.Open, plan.State);          // за замовчуванням Open
        Assert.Null(plan.OrderId);
        Assert.Null(plan.Order);
        Assert.Null(plan.Author);

        Assert.NotNull(plan.PlanActions);
        Assert.Empty(plan.PlanActions);

        Assert.Equal(DateTimeKind.Utc, plan.RecordedUtc.Kind);
        Assert.True(plan.RecordedUtc >= before && plan.RecordedUtc <= after);
    }

    [Fact]
    public void CanSet_PlanNumber_And_Author()
    {
        // Arrange
        var plan = new Plan();

        // Act
        plan.PlanNumber = "PL-001";
        plan.Author = "test.user";

        // Assert
        Assert.Equal("PL-001", plan.PlanNumber);
        Assert.Equal("test.user", plan.Author);
    }

    [Fact]
    public void State_CanChange_Open_To_Close()
    {
        // Arrange
        var plan = new Plan();

        // Act
        plan.State = PlanState.Close;

        // Assert
        Assert.Equal(PlanState.Close, plan.State);
    }

    [Fact]
    public void PlanActions_AddAction_AddsToCollection()
    {
        // Arrange
        var plan = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-002" };
        var personId = Guid.NewGuid();

        var action = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Plan = plan,
            PersonId = personId,
            Person = new Person { Id = personId, LastName = "Doe", FirstName = "John" },
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.UtcNow,
            Location = "Loc-1",
            GroupName = "Group-1",
            CrewName = "Crew-1",
            Rnokpp = "1234567890",
            FullName = "Doe John",
            RankName = "Rank",
            PositionName = "Position",
            BZVP = "Yes",
            Weapon = "W-1",
            Callsign = "C-1",
            StatusKindOnDate = "Active"
        };

        // Act
        plan.PlanActions.Add(action);

        // Assert
        Assert.Single(plan.PlanActions);
        Assert.Same(action, plan.PlanActions.First());
        Assert.Same(plan, action.Plan);
        Assert.Equal(plan.Id, action.PlanId);
    }

    [Fact]
    public void CanAttach_Order_ToPlan()
    {
        // Arrange
        var plan = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-003" };
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Name = "НК-2025/001",
            EffectiveMomentUtc = DateTime.UtcNow
        };

        // Act
        plan.Order = order;
        plan.OrderId = order.Id;

        // Assert
        Assert.Same(order, plan.Order);
        Assert.Equal(order.Id, plan.OrderId);
    }

    [Fact]
    public void PlanActions_Collections_AreIndependent_PerInstance()
    {
        // Arrange
        var p1 = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-010" };
        var p2 = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-011" };

        var a1 = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = p1.Id,
            Plan = p1,
            PersonId = Guid.NewGuid(),
            Person = new Person { Id = Guid.NewGuid(), LastName = "A", FirstName = "A" },
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.UtcNow,
            Location = "L",
            GroupName = "G",
            CrewName = "C",
            Rnokpp = "1111111111",
            FullName = "A A",
            RankName = "R",
            PositionName = "P",
            BZVP = "Y",
            Weapon = "W",
            Callsign = "CA",
            StatusKindOnDate = "Active"
        };

        // Act
        p1.PlanActions.Add(a1);

        // Assert
        Assert.Single(p1.PlanActions);
        Assert.Empty(p2.PlanActions);
    }
}