//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanServiceOptionsTests (domain model)
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models;

public class PlanServiceOptionsTests
{
    [Fact(DisplayName = "PlanServiceOptions: CLR-дефолти властивостей")]
    public void Defaults_Are_Clr()
    {
        var o = new PlanServiceOptions();

        Assert.Equal(0, o.Id);
        Assert.Null(o.DispatchStatusKindId);
        Assert.Null(o.ReturnStatusKindId);

        Assert.Null(o.DispatchStatusKind);
        Assert.Null(o.ReturnStatusKind);

        Assert.Null(o.Author);
        Assert.Equal(default, o.ModifiedUtc);
    }

    [Fact(DisplayName = "PlanServiceOptions: базові властивості читаються/записуються")]
    public void Can_Set_And_Read_Properties()
    {
        var o = new PlanServiceOptions
        {
            Id = 1,
            DispatchStatusKindId = 2,
            ReturnStatusKindId = 1,
            Author = "tester",
            ModifiedUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        Assert.Equal(1, o.Id);
        Assert.Equal(2, o.DispatchStatusKindId);
        Assert.Equal(1, o.ReturnStatusKindId);
        Assert.Equal("tester", o.Author);
        Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), o.ModifiedUtc);
    }

    [Fact(DisplayName = "PlanServiceOptions: навігаційні властивості призначаються коректно")]
    public void Can_Assign_Navigation()
    {
        var dispatch = new StatusKind { Id = 2, Name = "В БР", Code = "100", IsActive = true };
        var ret = new StatusKind { Id = 1, Name = "В районі", Code = "30", IsActive = true };

        var o = new PlanServiceOptions
        {
            Id = 1,
            DispatchStatusKindId = dispatch.Id,
            ReturnStatusKindId = ret.Id,
            DispatchStatusKind = dispatch,
            ReturnStatusKind = ret
        };

        Assert.Same(dispatch, o.DispatchStatusKind);
        Assert.Same(ret, o.ReturnStatusKind);
        Assert.Equal(2, o.DispatchStatusKindId);
        Assert.Equal(1, o.ReturnStatusKindId);
    }
}
