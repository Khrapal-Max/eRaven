//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// ApprovePlanActionViewModelTests
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanActionViewModels;

namespace eRaven.Tests.Application.Tests.ViewModels;

public class ApprovePlanActionViewModelTests
{
    [Fact]
    public void DefaultCtor_Should_Have_Expected_Defaults()
    {
        // Arrange
        var vm = new ApprovePlanActionViewModel();

        // Act
        // (no-op)

        // Assert
        Assert.Equal(Guid.Empty, vm.Id);
        Assert.Equal(Guid.Empty, vm.PersonId);
        Assert.Equal(default, vm.EffectiveAtUtc); // 01/01/0001 00:00:00
        Assert.Equal(string.Empty, vm.Order);
    }

    [Fact]
    public void Setters_Should_Assign_Values()
    {
        // Arrange
        var id = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var when = DateTime.SpecifyKind(new DateTime(2025, 9, 21, 12, 30, 0), DateTimeKind.Utc);
        var order = "БР-77/25";

        var vm = new ApprovePlanActionViewModel
        {
            // Act
            Id = id,
            PersonId = personId,
            EffectiveAtUtc = when,
            Order = order
        };

        // Assert
        Assert.Equal(id, vm.Id);
        Assert.Equal(personId, vm.PersonId);
        Assert.Equal(when, vm.EffectiveAtUtc);
        Assert.Equal(order, vm.Order);
    }

    [Fact]
    public void EffectiveAtUtc_Should_Allow_UtcKind()
    {
        // Arrange
        var utc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        var vm = new ApprovePlanActionViewModel
        {
            // Act
            EffectiveAtUtc = utc
        };

        // Assert
        Assert.Equal(DateTimeKind.Utc, vm.EffectiveAtUtc.Kind);
        Assert.Equal(utc, vm.EffectiveAtUtc);
    }
}
