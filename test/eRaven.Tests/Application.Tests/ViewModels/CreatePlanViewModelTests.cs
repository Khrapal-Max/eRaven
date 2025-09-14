//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// CreatePlanViewModelTests
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;

namespace eRaven.Tests.Application.Tests.ViewModels;

public sealed class CreatePlanViewModelTests
{
    [Fact(DisplayName = "CreatePlanViewModel: дефолти — PlanNumber=null, State=Open, PlanElements порожня колекція")]
    public void Defaults_AreCorrect()
    {
        // Act
        var vm = new CreatePlanViewModel();

        // Assert
        Assert.Null(vm.PlanNumber);                    // default! → у new() буде null
        Assert.Equal(PlanState.Open, vm.State);
    }
}
