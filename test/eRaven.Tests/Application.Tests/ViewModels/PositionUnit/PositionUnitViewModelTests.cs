//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitViewModelTests
//-----------------------------------------------------------------------------


//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitViewModelTests
//-----------------------------------------------------------------------------

namespace eRaven.Tests.Application.Tests.ViewModels.PositionUnit;

public class PositionUnitViewModelTests
{
    [Fact]
    public void Default_Values_Are_Correct()
    {
        var vm = new PositionUnitViewModel();

        Assert.Equal(Guid.Empty, vm.Id);
        Assert.Equal(string.Empty, vm.Code);
        Assert.Equal(string.Empty, vm.ShortName);
        Assert.Equal(string.Empty, vm.SpecialNumber);
        Assert.Equal(string.Empty, vm.FullName);
        Assert.Null(vm.CurrentPersonFullName);
        Assert.False(vm.IsActived);
    }

    [Fact]
    public void Can_Assign_And_Read_Back_Properties()
    {
        var id = Guid.NewGuid();

        var vm = new PositionUnitViewModel
        {
            Id = id,
            Code = "A1",
            ShortName = "Посада",
            SpecialNumber = "12-345",
            FullName = "Повна назва посади",
            CurrentPersonFullName = "Іван Іванов",
            IsActived = true
        };

        Assert.Equal(id, vm.Id);
        Assert.Equal("A1", vm.Code);
        Assert.Equal("Посада", vm.ShortName);
        Assert.Equal("12-345", vm.SpecialNumber);
        Assert.Equal("Повна назва посади", vm.FullName);
        Assert.Equal("Іван Іванов", vm.CurrentPersonFullName);
        Assert.True(vm.IsActived);
    }
}