//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// AssignViewModelTests
//-----------------------------------------------------------------------------


//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// AssignViewModelTests
//-----------------------------------------------------------------------------

using eRaven.Components.Pages.PositionAssignments.Modals;

namespace eRaven.Tests.Application.Tests.ViewModels.PositionUnit;

public class AssignViewModelTests
{
    [Fact(DisplayName = "Default ctor: порожні ідентифікатори та null для Note")]
    public void DefaultCtor_HasExpectedDefaults()
    {
        // Arrange
        var vm = new AssignViewModel();

        // Assert
        Assert.Equal(Guid.Empty, vm.PersonId);
        Assert.Equal(Guid.Empty, vm.PositionUnitId);
        Assert.Null(vm.Note);
    }

    [Fact(DisplayName = "Setters: властивості коректно присвоюються")]
    public void Setters_AssignValues()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var note = "Призначено наказом №123";

        // Act
        var vm = new AssignViewModel
        {
            PersonId = personId,
            PositionUnitId = positionId,
            Note = note
        };

        // Assert
        Assert.Equal(personId, vm.PersonId);
        Assert.Equal(positionId, vm.PositionUnitId);
        Assert.Equal(note, vm.Note);
    }

    [Theory(DisplayName = "Note: допускає null, порожній та пробільний рядок")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Note_AllowsNullOrWhitespace(string? note)
    {
        // Arrange
        var vm = new AssignViewModel
        {
            PersonId = Guid.NewGuid(),
            PositionUnitId = Guid.NewGuid(),
            Note = note
        };

        // Assert
        Assert.Equal(note, vm.Note);
    }
}
