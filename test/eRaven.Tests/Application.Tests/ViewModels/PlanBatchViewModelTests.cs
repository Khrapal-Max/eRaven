//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanBatchViewModelTests
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;

namespace eRaven.Tests.Application.Tests.ViewModels;

public class PlanBatchViewModelTests
{
    [Fact]
    public void Ctor_Defaults_Are_Expected()
    {
        var vm = new PlanBatchViewModel();

        // reference-тип з '= default!' фактично null до присвоєння
        Assert.Null(vm.PlanNumber);

        // Поле Actions ініціалізоване до порожнього списку
        Assert.NotNull(vm.Actions);
        Assert.Empty(vm.Actions);
    }

    [Fact]
    public void Can_Set_And_Get_PlanNumber()
    {
        var vm = new PlanBatchViewModel { PlanNumber = "PLN-2025-007" };

        Assert.Equal("PLN-2025-007", vm.PlanNumber);
    }

    [Fact]
    public void Can_Assign_Actions_List_And_Read_Back()
    {
        var list = new List<PlanActionViewModel>
            {
                new()
                {
                    PlanNumber = "PLN-2025-007",
                    PersonId   = Guid.NewGuid(),
                    ActionType = PlanActionType.Dispatch,
                    EventAtUtc = DateTime.UtcNow,
                    Location   = "Локація-A",
                    GroupName  = "Група-1",
                    CrewName   = "Екіпаж-A",
                    Note       = "планова дія"
                }
            };

        var vm = new PlanBatchViewModel
        {
            PlanNumber = "PLN-2025-007",
            Actions = list    // поле, тип IReadOnlyList<PlanActionViewModel>, але присвоюємо List<>
        };

        Assert.Same(list, vm.Actions);
        Assert.Single(vm.Actions);
        Assert.Equal("Локація-A", vm.Actions[0].Location);
    }

    [Fact]
    public void Actions_Field_Can_Be_Reassigned()
    {
        var first = new List<PlanActionViewModel>();
        var second = new List<PlanActionViewModel> { new() { PlanNumber = "X", Location = "A", GroupName = "G", CrewName = "C" } };

        var vm = new PlanBatchViewModel { Actions = first };
        Assert.Same(first, vm.Actions);
        Assert.Empty(vm.Actions);

        vm.Actions = second; // перевіряємо, що поле можна перепризначити
        Assert.Same(second, vm.Actions);
        Assert.Single(vm.Actions);
    }

    [Fact]
    public void Actions_Field_Allows_Null_Assignment_But_Is_Null_Thereafter()
    {
        var vm = new PlanBatchViewModel();
        Assert.NotNull(vm.Actions); // за замовчуванням — порожній список

        vm.Actions = null!; // технічно дозволено, бо це публічне поле

        Assert.Null(vm.Actions);
    }
}
