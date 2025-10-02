//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// LocationGroupViewModelTests
//-----------------------------------------------------------------------------

namespace eRaven.Tests.Application.Tests.ViewModels.Reports;

public class LocationGroupViewModelTests
{
    [Fact(DisplayName = "LocationGroupViewModel: значення за замовчуванням коректні")]
    public void Defaults_Are_Correct()
    {
        // Act
        var vm = new LocationGroupViewModel();

        // Assert
        Assert.Equal(string.Empty, vm.Location);
        Assert.NotNull(vm.Groups);
        Assert.Empty(vm.Groups);
    }

    [Fact(DisplayName = "LocationGroupViewModel: можна встановити Location і додати групи")]
    public void Can_Set_Location_And_Add_Groups()
    {
        // Arrange
        var vm = new LocationGroupViewModel
        {
            // Act
            Location = "Sector Alpha"
        };
        vm.Groups.Add(new GroupViewModel { GroupName = "Group A" });
        vm.Groups.Add(new GroupViewModel { GroupName = "Group B" });

        // Assert
        Assert.Equal("Sector Alpha", vm.Location);
        Assert.Equal(2, vm.Groups.Count);
        Assert.Contains(vm.Groups, g => g.GroupName == "Group A");
        Assert.Contains(vm.Groups, g => g.GroupName == "Group B");
    }

    [Fact(DisplayName = "LocationGroupViewModel: колекція Groups змінювана, але посилання не змінюється")]
    public void Groups_Is_Mutable_List_But_Not_Settable()
    {
        // Arrange
        var vm = new LocationGroupViewModel();
        var refBefore = vm.Groups;

        // Act
        vm.Groups.Add(new GroupViewModel { GroupName = "G-1" });
        vm.Groups.Add(new GroupViewModel { GroupName = "G-2" });
        var refAfter = vm.Groups;

        // Assert
        Assert.Same(refBefore, refAfter);
        Assert.Equal(2, vm.Groups.Count);
    }

    [Fact(DisplayName = "LocationGroupViewModel: можна вкладено додавати екіпажі та людей")]
    public void Can_Add_Nested_Crews_And_Rows()
    {
        // Arrange
        var vm = new LocationGroupViewModel { Location = "Base-1" };
        var group = new GroupViewModel { GroupName = "Group-X" };
        var crew = new CrewGroupViewModel { CrewName = "Crew-7" };

        // Act
        crew.Rows.Add(new PlanniingOnDateRowViewModel { FullName = "Іван Іваненко", RankName = "Сержант" });
        crew.Rows.Add(new PlanniingOnDateRowViewModel { FullName = "Петро Петренко", RankName = "Солдат" });
        group.Crews.Add(crew);
        vm.Groups.Add(group);

        // Assert
        Assert.Single(vm.Groups);
        Assert.Equal("Group-X", vm.Groups[0].GroupName);
        Assert.Single(vm.Groups[0].Crews);
        Assert.Equal("Crew-7", vm.Groups[0].Crews[0].CrewName);
        Assert.Equal(2, vm.Groups[0].Crews[0].Rows.Count);
        Assert.Contains(vm.Groups[0].Crews[0].Rows, r => r.FullName == "Іван Іваненко" && r.RankName == "Сержант");
        Assert.Contains(vm.Groups[0].Crews[0].Rows, r => r.FullName == "Петро Петренко" && r.RankName == "Солдат");
    }
}
