//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// GroupViewModelTests
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanningOnDateViewModels;

namespace eRaven.Tests.Application.Tests.ViewModels.Reports;

public class GroupViewModelTests
{
    [Fact(DisplayName = "GroupViewModel: значення за замовчуванням коректні")]
    public void Defaults_Are_Correct()
    {
        // Act
        var vm = new GroupViewModel();

        // Assert
        Assert.Equal(string.Empty, vm.GroupName);
        Assert.NotNull(vm.Crews);
        Assert.Empty(vm.Crews);
    }

    [Fact(DisplayName = "GroupViewModel: можна встановити GroupName і додати екіпажі")]
    public void Can_Set_GroupName_And_Add_Crews()
    {
        // Arrange
        var vm = new GroupViewModel();

        // Act
        vm.GroupName = "Group Alpha";
        vm.Crews.Add(new CrewGroupViewModel { CrewName = "Crew 1" });
        vm.Crews.Add(new CrewGroupViewModel { CrewName = "Crew 2" });

        // Assert
        Assert.Equal("Group Alpha", vm.GroupName);
        Assert.Equal(2, vm.Crews.Count);
        Assert.Contains(vm.Crews, c => c.CrewName == "Crew 1");
        Assert.Contains(vm.Crews, c => c.CrewName == "Crew 2");
    }

    [Fact(DisplayName = "GroupViewModel: колекція Crews змінювана, але посилання не змінюється")]
    public void Crews_Is_Mutable_List_But_Not_Settable()
    {
        // Arrange
        var vm = new GroupViewModel();
        var referenceBefore = vm.Crews;

        // Act
        vm.Crews.Add(new CrewGroupViewModel { CrewName = "Crew X" });
        vm.Crews.Add(new CrewGroupViewModel { CrewName = "Crew Y" });
        var referenceAfter = vm.Crews;

        // Assert
        Assert.Same(referenceBefore, referenceAfter);
        Assert.Equal(2, vm.Crews.Count);
    }

    [Fact(DisplayName = "GroupViewModel: можна вкладено додавати рядки людей до екіпажів")]
    public void Can_Add_Rows_Into_Crews_Nested()
    {
        // Arrange
        var vm = new GroupViewModel { GroupName = "G" };
        var crew = new CrewGroupViewModel { CrewName = "C-1" };

        // Act
        crew.Rows.Add(new PlanniingOnDateRowViewModel { FullName = "Іван Іваненко" });
        crew.Rows.Add(new PlanniingOnDateRowViewModel { FullName = "Петро Петренко" });
        vm.Crews.Add(crew);

        // Assert
        Assert.Single(vm.Crews);
        Assert.Equal("C-1", vm.Crews[0].CrewName);
        Assert.Equal(2, vm.Crews[0].Rows.Count);
        Assert.Contains(vm.Crews[0].Rows, r => r.FullName == "Іван Іваненко");
        Assert.Contains(vm.Crews[0].Rows, r => r.FullName == "Петро Петренко");
    }
}
