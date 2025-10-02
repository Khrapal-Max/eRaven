//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ApprovePlanActionViewModelValidatorTests
//-----------------------------------------------------------------------------

namespace eRaven.Tests.Components.Tests.Pages.Plans;

public class ApprovePlanActionViewModelValidatorTests
{
    private static ApprovePlanActionViewModel ValidModel()
    {
        return new ApprovePlanActionViewModel
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            EffectiveAtUtc = DateTime.SpecifyKind(new DateTime(2025, 9, 21, 12, 0, 0), DateTimeKind.Utc),
            Order = "БР-77/25"
        };
    }

    [Fact]
    public void Validate_ValidModel_Should_Pass()
    {
        // Arrange
        var vm = ValidModel();
        var validator = new ApprovePlanActionViewModelValidator();

        // Act
        var result = validator.Validate(vm);

        // Assert
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void Validate_EmptyId_Should_Fail()
    {
        // Arrange
        var vm = ValidModel();
        vm.Id = Guid.Empty;
        var validator = new ApprovePlanActionViewModelValidator();

        // Act
        var result = validator.Validate(vm);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(vm.Id));
    }

    [Fact]
    public void Validate_EmptyPersonId_Should_Fail()
    {
        // Arrange
        var vm = ValidModel();
        vm.PersonId = Guid.Empty;
        var validator = new ApprovePlanActionViewModelValidator();

        // Act
        var result = validator.Validate(vm);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(vm.PersonId));
    }

    [Fact]
    public void Validate_DefaultEffectiveAt_Should_Fail()
    {
        // Arrange
        var vm = ValidModel();
        vm.EffectiveAtUtc = default;
        var validator = new ApprovePlanActionViewModelValidator();

        // Act
        var result = validator.Validate(vm);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(vm.EffectiveAtUtc));
    }

    [Fact]
    public void Validate_NonUtcEffectiveAt_Should_Fail()
    {
        // Arrange
        var vm = ValidModel();
        vm.EffectiveAtUtc = new DateTime(2025, 9, 21, 12, 0, 0, DateTimeKind.Local);
        var validator = new ApprovePlanActionViewModelValidator();

        // Act
        var result = validator.Validate(vm);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(vm.EffectiveAtUtc));
    }

    [Fact]
    public void Validate_OutOfRangeYear_Should_Fail()
    {
        // Arrange
        var vm = ValidModel();
        vm.EffectiveAtUtc = DateTime.SpecifyKind(new DateTime(1999, 12, 31, 23, 59, 0), DateTimeKind.Utc);
        var validator = new ApprovePlanActionViewModelValidator();

        // Act
        var result = validator.Validate(vm);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(vm.EffectiveAtUtc));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceOrder_Should_Fail(string? order)
    {
        // Arrange
        var vm = ValidModel();
        vm.Order = order ?? string.Empty;
        var validator = new ApprovePlanActionViewModelValidator();

        // Act
        var result = validator.Validate(vm);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(vm.Order));
    }

    [Fact]
    public void Validate_OrderTooLong_Should_Fail()
    {
        // Arrange
        var vm = ValidModel();
        vm.Order = new string('X', 513); // > 128
        var validator = new ApprovePlanActionViewModelValidator();

        // Act
        var result = validator.Validate(vm);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(vm.Order));
    }

    [Fact]
    public void Validate_OrderMaxLength128_Should_Pass()
    {
        // Arrange
        var vm = ValidModel();
        vm.Order = new string('X', 512);
        var validator = new ApprovePlanActionViewModelValidator();

        // Act
        var result = validator.Validate(vm);

        // Assert
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }
}
