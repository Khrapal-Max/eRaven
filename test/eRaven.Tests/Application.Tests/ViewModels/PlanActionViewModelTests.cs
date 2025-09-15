//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActionViewModelTests
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;

namespace eRaven.Tests.Application.Tests.ViewModels;

public class PlanActionViewModelTests
{
    [Fact]
    public void Ctor_Defaults_Are_Nulls_Or_Defaults()
    {
        var vm = new PlanActionViewModel();

        // reference-типи з '= default!' фактично null до присвоєння
        Assert.Null(vm.PlanNumber);
        Assert.Null(vm.Location);
        Assert.Null(vm.GroupName);
        Assert.Null(vm.CrewName);
        Assert.Null(vm.Note);

        // value-типи мають дефолти
        Assert.Equal(default, vm.PersonId);
        Assert.Equal(default, vm.ActionType);
        Assert.Equal(default, vm.EventAtUtc);
    }

    [Fact]
    public void Can_Set_And_Get_Core_Fields()
    {
        var planNumber = "PLN-2025-001";
        var personId = Guid.NewGuid();
        var actionType = PlanActionType.Dispatch;
        var whenUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        var vm = new PlanActionViewModel
        {
            PlanNumber = planNumber,
            PersonId = personId,
            ActionType = actionType,
            EventAtUtc = whenUtc
        };

        Assert.Equal(planNumber, vm.PlanNumber);
        Assert.Equal(personId, vm.PersonId);
        Assert.Equal(actionType, vm.ActionType);
        Assert.Equal(whenUtc, vm.EventAtUtc);
        Assert.Equal(DateTimeKind.Utc, vm.EventAtUtc.Kind); // ми самі задали UTC
    }

    [Fact]
    public void Can_Set_And_Get_Context_Fields()
    {
        var vm = new PlanActionViewModel
        {
            Location = "Локація-А",
            GroupName = "Група-1",
            CrewName = "Екіпаж-А",
            Note = "коментар"
        };

        Assert.Equal("Локація-А", vm.Location);
        Assert.Equal("Група-1", vm.GroupName);
        Assert.Equal("Екіпаж-А", vm.CrewName);
        Assert.Equal("коментар", vm.Note);
    }

    [Fact]
    public void EventAtUtc_Allows_Local_Or_Unspecified_Kinds_VM_Does_Not_Enforce()
    {
        // VM не конвертує час — перевіряємо, що значення зберігається як є.
        var local = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var unspecified = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

        var vmLocal = new PlanActionViewModel { EventAtUtc = local };
        var vmUnspec = new PlanActionViewModel { EventAtUtc = unspecified };

        Assert.Equal(local, vmLocal.EventAtUtc);
        Assert.Equal(DateTimeKind.Local, vmLocal.EventAtUtc.Kind);

        Assert.Equal(unspecified, vmUnspec.EventAtUtc);
        Assert.Equal(DateTimeKind.Unspecified, vmUnspec.EventAtUtc.Kind);
    }
}
