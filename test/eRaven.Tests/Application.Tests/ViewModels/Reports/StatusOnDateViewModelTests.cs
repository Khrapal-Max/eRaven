// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// StatusOnDateViewModelTests
// -----------------------------------------------------------------------------


// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// StatusOnDateViewModelTests
// -----------------------------------------------------------------------------

using eRaven.Application.ViewModels.StaffOnDateViewModels;

namespace eRaven.Tests.Application.Tests.ViewModels.Reports;

public class StatusOnDateViewModelTests
{
    [Fact(DisplayName = "StatusOnDateViewModel: за замовчуванням усі властивості null")]
    public void Defaults_Are_Null()
    {
        var vm = new StatusOnDateViewModel();

        Assert.Null(vm.Code);
        Assert.Null(vm.Name);
        Assert.Null(vm.Note);
    }

    [Fact(DisplayName = "StatusOnDateViewModel: встановлення/зчитування властивостей працює")]
    public void Setters_Getters_Work()
    {
        var vm = new StatusOnDateViewModel
        {
            Code = "ВДР",
            Name = "Відрядження",
            Note = "На навчанні"
        };

        Assert.Equal("ВДР", vm.Code);
        Assert.Equal("Відрядження", vm.Name);
        Assert.Equal("На навчанні", vm.Note);
    }
}
