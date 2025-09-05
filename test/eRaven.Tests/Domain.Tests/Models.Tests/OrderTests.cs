//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// OrderTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Tests.Domain.Tests.Models.Tests.Helpers;

namespace eRaven.Tests.Domain.Tests.Models.Tests;

public class OrderTests
{
    [Fact]
    public void CreateFromPlan_Sets_PlanId_And_EffectiveMoment_From_Plan()
    {
        // arrange
        var plan = OrderTestsHelpers.MakePlan(timeKind: PlanTimeKind.Start);
        var nowUtc = new DateTime(2025, 01, 01, 17, 31, 0, DateTimeKind.Utc);

        // act
        var order = OrderTestsHelpers.CreateFromPlan(plan, "Наказ №1", "moderator", nowUtc);

        // assert
        Assert.Equal(plan.Id, order.PlanId);
        Assert.Equal(plan.PlannedAtUtc, order.EffectiveMomentUtc);
        Assert.Equal("Наказ №1", order.Name);
        Assert.Equal("moderator", order.Author);
        Assert.Equal(nowUtc, order.RecordedUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateFromPlan_Throws_When_Name_Is_Empty(string? badName)
    {
        var plan = OrderTestsHelpers.MakePlan();
        var nowUtc = new DateTime(2025, 01, 01, 18, 00, 0, DateTimeKind.Utc);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            OrderTestsHelpers.CreateFromPlan(plan, badName!, "moderator", nowUtc));

        Assert.Contains("Name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateFromPlan_Throws_When_Plan_Is_Null()
    {
        var nowUtc = new DateTime(2025, 01, 01, 18, 00, 0, DateTimeKind.Utc);

        var ex = Assert.Throws<ArgumentNullException>(() =>
            OrderTestsHelpers.CreateFromPlan(null!, "Наказ №1", "moderator", nowUtc));

        Assert.Equal("plan", ex.ParamName);
    }

    [Fact]
    public void EffectiveMoment_Is_Snapshot_And_Does_Not_Change_If_Plan_Is_Modified_Later()
    {
        // arrange
        var plan = OrderTestsHelpers.MakePlan(plannedAtUtc: new DateTime(2025, 01, 01, 17, 30, 0, DateTimeKind.Utc));
        var order = OrderTestsHelpers.CreateFromPlan(plan, "Наказ №2", "moderator",
            new DateTime(2025, 01, 01, 17, 31, 0, DateTimeKind.Utc));

        // mutate plan after order creation
        plan.PlannedAtUtc = new DateTime(2025, 01, 01, 17, 45, 0, DateTimeKind.Utc);

        // assert snapshot stayed the same
        Assert.Equal(new DateTime(2025, 01, 01, 17, 30, 0, DateTimeKind.Utc), order.EffectiveMomentUtc);
    }

    [Fact]
    public void CreateFromPlan_Uses_TimeKind_Semantics_But_EffectiveMoment_Comes_From_Plan()
    {
        // arrange: Return/End — семантика “кінець”, але значення беремо з Plan.PlannedAtUtc
        var plan = OrderTestsHelpers.MakePlan(
            type: PlanType.Return,
            timeKind: PlanTimeKind.End,
            plannedAtUtc: new DateTime(2025, 02, 10, 09, 45, 0, DateTimeKind.Utc)
        );

        // act
        var order = OrderTestsHelpers.CreateFromPlan(plan, "Наказ №3", "moderator",
            new DateTime(2025, 02, 10, 09, 50, 0, DateTimeKind.Utc));

        // assert
        Assert.Equal(plan.PlannedAtUtc, order.EffectiveMomentUtc);
        Assert.Equal(plan.Id, order.PlanId);
    }
}
