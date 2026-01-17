//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// VoidPersonEventDtoValidatorTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Validations.Personal;
using FluentValidation.TestHelper;

namespace eRaven.Tests.Application.Validations.Personal;

public sealed class VoidPersonEventDtoValidatorTests
{
    private readonly VoidPersonEventDtoValidator _validator = new();

    [Fact]
    public void TargetEventId_empty_should_have_error()
    {
        // arrange
        var dto = new VoidPersonEventDto
        {
            TargetEventId = Guid.Empty,
            Reason = "Причина"
        };

        // act
        var result = _validator.TestValidate(dto);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.TargetEventId)
            .WithErrorMessage("Вкажіть подію для корекції.");
    }

    [Fact]
    public void TargetEventId_non_empty_should_not_have_error()
    {
        // arrange
        var dto = new VoidPersonEventDto
        {
            TargetEventId = Guid.NewGuid(),
            Reason = "Причина"
        };

        // act
        var result = _validator.TestValidate(dto);

        // assert
        result.ShouldNotHaveValidationErrorFor(x => x.TargetEventId);
    }

    [Fact]
    public void Reason_empty_should_have_error()
    {
        // arrange
        var dto = new VoidPersonEventDto
        {
            TargetEventId = Guid.NewGuid(),
            Reason = ""
        };

        // act
        var result = _validator.TestValidate(dto);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Вкажіть причину корекції.");
    }

    [Fact]
    public void Reason_null_should_have_error()
    {
        // arrange
        var dto = new VoidPersonEventDto
        {
            TargetEventId = Guid.NewGuid(),
            Reason = null
        };

        // act
        var result = _validator.TestValidate(dto);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Вкажіть причину корекції.");
    }

    [Fact]
    public void Reason_too_long_should_have_error()
    {
        // arrange
        var dto = new VoidPersonEventDto
        {
            TargetEventId = Guid.NewGuid(),
            Reason = new string('a', 513)
        };

        // act
        var result = _validator.TestValidate(dto);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Причина занадто довга (макс. 512).");
    }

    [Fact]
    public void Reason_max_length_512_should_be_valid()
    {
        // arrange
        var dto = new VoidPersonEventDto
        {
            TargetEventId = Guid.NewGuid(),
            Reason = new string('a', 512)
        };

        // act
        var result = _validator.TestValidate(dto);

        // assert
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Reason_should_stop_on_empty_and_not_check_max_length()
    {
        // arrange
        var dto = new VoidPersonEventDto
        {
            TargetEventId = Guid.NewGuid(),
            Reason = "" // empty triggers NotEmpty, Cascade Stop should prevent further validators
        };

        // act
        var result = _validator.TestValidate(dto);

        // assert
        var errors = result.Errors.Where(e => e.PropertyName == nameof(VoidPersonEventDto.Reason)).ToList();
        Assert.Single(errors);
        Assert.Equal("Вкажіть причину корекції.", errors[0].ErrorMessage);
    }

    [Fact]
    public void Valid_dto_should_have_no_errors()
    {
        // arrange
        var dto = new VoidPersonEventDto
        {
            TargetEventId = Guid.NewGuid(),
            Reason = "Помилка в посаді, виправлення наказу №240"
        };

        // act
        var result = _validator.TestValidate(dto);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
