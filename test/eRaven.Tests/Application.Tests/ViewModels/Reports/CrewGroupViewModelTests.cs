//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// CrewGroupViewModelTests
//-----------------------------------------------------------------------------

namespace eRaven.Tests.Application.Tests.ViewModels.Reports;

public class CrewGroupViewModelTests
{
    [Fact(DisplayName = "CrewGroupViewModel: значення за замовчуванням коректні")]
    public void Defaults_Are_Correct()
    {
        // Act
        var vm = new CrewGroupViewModel();

        // Assert
        Assert.Equal(string.Empty, vm.CrewName);
        Assert.NotNull(vm.Rows);
        Assert.Empty(vm.Rows);
    }

    [Fact(DisplayName = "CrewGroupViewModel: можна встановити CrewName і додати рядки")]
    public void Can_Set_CrewName_And_Add_Rows()
    {
        // Arrange
        var vm = new CrewGroupViewModel();

        // Act
        vm.CrewName = "Crew A";
        vm.Rows.Add(new PlanniingOnDateRowViewModel { FullName = "Іваненко Іван" });
        vm.Rows.Add(new PlanniingOnDateRowViewModel { FullName = "Петров Петро" });

        // Assert
        Assert.Equal("Crew A", vm.CrewName);
        Assert.Equal(2, vm.Rows.Count);
        Assert.Contains(vm.Rows, r => r.FullName == "Іваненко Іван");
        Assert.Contains(vm.Rows, r => r.FullName == "Петров Петро");
    }

    [Fact(DisplayName = "CrewGroupViewModel: список Rows доступний для модифікації, але не перезаписується")]
    public void Rows_Is_Mutable_List_But_Not_Settable()
    {
        // Arrange
        var vm = new CrewGroupViewModel();
        var referenceBefore = vm.Rows;

        // Act
        vm.Rows.Add(new PlanniingOnDateRowViewModel { FullName = "Тест Тест" });
        var referenceAfter = vm.Rows;

        // Assert
        Assert.Same(referenceBefore, referenceAfter); // та сама колекція
        Assert.Single(vm.Rows);
    }
}
