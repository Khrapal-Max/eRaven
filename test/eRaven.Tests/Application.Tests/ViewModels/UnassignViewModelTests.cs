//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// UnassignViewModelTests
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PositionAssignmentViewModels;

namespace eRaven.Tests.Application.Tests.ViewModels;

public class UnassignViewModelTests
{
    [Fact(DisplayName = "Default ctor: PersonId = Guid.Empty, Note = null")]
    public void DefaultCtor_HasExpectedDefaults()
    {
        // Arrange
        var vm = new UnassignViewModel();

        // Assert
        Assert.Equal(Guid.Empty, vm.PersonId);
        Assert.Null(vm.Note);
    }

    [Fact(DisplayName = "Setters: властивості коректно присвоюються")]
    public void Setters_AssignValues()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var note = "Зняття з посади за наказом №321";

        // Act
        var vm = new UnassignViewModel
        {
            PersonId = personId,
            Note = note
        };

        // Assert
        Assert.Equal(personId, vm.PersonId);
        Assert.Equal(note, vm.Note);
    }

    [Theory(DisplayName = "Note: допускає null, порожній та пробільний рядок")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Note_AllowsNullOrWhitespace(string? note)
    {
        // Arrange
        var vm = new UnassignViewModel
        {
            PersonId = Guid.NewGuid(),
            Note = note
        };

        // Assert
        Assert.Equal(note, vm.Note);
    }
}
