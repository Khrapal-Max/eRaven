//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcludeDtoValidatorTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;
using eRaven.Application.Validations.Personal;
using FluentValidation.TestHelper;

namespace eRaven.Tests.Application.Validations.Personal;

public sealed class ExcludeDtoValidatorTests
{
    private readonly ExcludeDtoValidator _validator = new();

    private static ExcludeDto Valid()
        => new()
        {
            EffectiveDate = new DateOnly(2026, 01, 20),
            Reason = "Підстава"
        };

    [Fact]
    public void Valid_model_should_pass()
    {
        var model = Valid();

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EffectiveDate_empty_should_fail()
    {
        var model = Valid();
        model.EffectiveDate = default; // DateOnly.MinValue => NotEmpty fails

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.EffectiveDate)
            .WithErrorMessage("Вкажіть дату виключення.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reason_empty_should_fail(string? reason)
    {
        var model = Valid();
        model.Reason = reason!;

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Вкажіть підставу.");
    }

    [Fact]
    public void Reason_too_long_should_fail()
    {
        var model = Valid();
        model.Reason = new string('x', 513);

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Підстава занадто довга (макс. 512).");
    }

    [Fact]
    public void Reason_max_length_512_should_pass()
    {
        var model = Valid();
        model.Reason = new string('x', 512);

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }
}
