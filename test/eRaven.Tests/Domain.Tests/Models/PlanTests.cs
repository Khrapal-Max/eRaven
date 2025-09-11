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
    [Fact(DisplayName = "Plan: дефолти — Id/PlanNumber/Author порожні, State=Open, RecordedUtc ~ now (UTC), PlanElements порожній")]
    public void Defaults_AreCorrect_ForNewPlan()
    {
        // Act
        var p = new Plan();

        // Assert
        Assert.Equal(Guid.Empty, p.Id);
        Assert.Null(p.PlanNumber);                 // default! у класі → у new() буде null
        Assert.Equal(PlanState.Open, p.State);
        Assert.Null(p.Author);

        // RecordedUtc ініціалізується DateTime.UtcNow у властивості
        Assert.Equal(DateTimeKind.Utc, p.RecordedUtc.Kind);
        Assert.InRange(DateTime.UtcNow - p.RecordedUtc, TimeSpan.Zero, TimeSpan.FromSeconds(5));

        Assert.NotNull(p.PlanElements);
        Assert.Empty(p.PlanElements);
    }

    [Fact(DisplayName = "Plan: можна задати та прочитати базові властивості")]
    public void Can_Set_And_Read_Properties()
    {
        var id = Guid.NewGuid();
        var when = new DateTime(2025, 9, 10, 12, 0, 0, DateTimeKind.Utc);

        var p = new Plan
        {
            Id = id,
            PlanNumber = "R10/1CN",
            State = PlanState.Close,
            Author = "tester",
            RecordedUtc = when
        };

        Assert.Equal(id, p.Id);
        Assert.Equal("R10/1CN", p.PlanNumber);
        Assert.Equal(PlanState.Close, p.State);
        Assert.Equal("tester", p.Author);
        Assert.Equal(when, p.RecordedUtc);
    }

    [Fact(DisplayName = "Plan: PlanElements — додавання/видалення елементів працює")]
    public void PlanElements_Add_Remove_Works()
    {
        var p = new Plan { PlanNumber = "P-001" };

        var e1 = new PlanElement { Id = Guid.NewGuid() };
        var e2 = new PlanElement { Id = Guid.NewGuid() };

        p.PlanElements.Add(e1);
        p.PlanElements.Add(e2);

        Assert.Equal(2, p.PlanElements.Count);
        Assert.Contains(e1, p.PlanElements);
        Assert.Contains(e2, p.PlanElements);

        p.PlanElements.Remove(e1);

        Assert.Single(p.PlanElements);
        Assert.DoesNotContain(e1, p.PlanElements);
        Assert.Contains(e2, p.PlanElements);
    }

    [Fact(DisplayName = "Plan: RecordedUtc можна задати явно (UTC)")]
    public void RecordedUtc_CanBeAssigned_Explicitly()
    {
        var ts = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var p = new Plan { RecordedUtc = ts };

        Assert.Equal(DateTimeKind.Utc, p.RecordedUtc.Kind);
        Assert.Equal(ts, p.RecordedUtc);
    }
}
