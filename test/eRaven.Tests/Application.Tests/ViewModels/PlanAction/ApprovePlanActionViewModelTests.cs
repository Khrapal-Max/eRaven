//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// ApprovePlanActionViewModelTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Tests.Application.Tests.ViewModels.PlanAction;

public class ApprovePlanActionViewModelTests
{
    [Fact]
    public void DefaultCtor_Should_Have_Expected_Defaults()
    {
        // Arrange + Act
        var vm = new ApprovePlanActionViewModel();

        // Assert
        Assert.Equal(Guid.Empty, vm.Id);
        Assert.Equal(Guid.Empty, vm.PersonId);
        Assert.Equal(default, vm.EffectiveAtUtc);           // 01/01/0001 00:00:00
        Assert.Equal(string.Empty, vm.Order);
        Assert.Equal(default, vm.MoveType);       // нове поле: дефолт
    }

    [Fact]
    public void Setters_Should_Assign_Values()
    {
        // Arrange
        var id = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var when = DateTime.SpecifyKind(new DateTime(2025, 9, 21, 12, 30, 0), DateTimeKind.Utc);
        const string order = "БР-77/25";

        // Act
        var vm = new ApprovePlanActionViewModel
        {
            Id = id,
            PersonId = personId,
            EffectiveAtUtc = when,
            Order = order,
            MoveType = MoveType.Return                   // нове поле: присвоєння
        };

        // Assert
        Assert.Equal(id, vm.Id);
        Assert.Equal(personId, vm.PersonId);
        Assert.Equal(when, vm.EffectiveAtUtc);
        Assert.Equal(order, vm.Order);
        Assert.Equal(MoveType.Return, vm.MoveType);        // перевірка
    }

    [Fact]
    public void EffectiveAtUtc_Should_Allow_UtcKind()
    {
        // Arrange
        var utc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        // Act
        var vm = new ApprovePlanActionViewModel { EffectiveAtUtc = utc };

        // Assert
        Assert.Equal(DateTimeKind.Utc, vm.EffectiveAtUtc.Kind);
        Assert.Equal(utc, vm.EffectiveAtUtc);
    }

    [Fact]
    public void MoveType_Should_Default_To_Dispatch_And_Be_Settable()
    {
        // Arrange
        var vm = new ApprovePlanActionViewModel();

        // Assert (default)
        Assert.Equal(default, vm.MoveType);

        // Act
        vm.MoveType = MoveType.Return;

        // Assert (set)
        Assert.Equal(MoveType.Return, vm.MoveType);
    }
}
