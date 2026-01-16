//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangePositionDtoValidatorTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Validations;
using FluentValidation.TestHelper;

namespace eRaven.Tests.Application.Validations;

public sealed class ChangePositionDtoValidatorTests
{
    private readonly ChangePositionDtoValidator _sut = new();

    [Fact]
    public void EffectiveDate_should_be_required()
    {
        var dto = new ChangePositionDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = default,
            PositionSort = null,
            Position = null,
            Note = null
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.EffectiveDate)
              .WithErrorMessage("Вкажіть дату.");
    }

    [Fact]
    public void Position_may_be_empty_and_should_not_trigger_errors()
    {
        var dto = new ChangePositionDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 01, 10),
            PositionSort = null,
            Position = null,
            Note = null
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Position);
        result.ShouldNotHaveValidationErrorFor(x => x.PositionSort);
    }

    [Fact]
    public void Position_whitespace_should_not_require_PositionSort_and_should_not_trigger_Position_errors()
    {
        var dto = new ChangePositionDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 01, 10),
            PositionSort = 0,     // навіть 0 не має валити, бо Position whitespace
            Position = "   ",
            Note = null
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Position);
        result.ShouldNotHaveValidationErrorFor(x => x.PositionSort);
    }

    [Fact]
    public void Position_should_not_exceed_512_chars_when_provided()
    {
        var dto = new ChangePositionDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 01, 10),
            PositionSort = 1,
            Position = new string('P', 513),
            Note = null
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Position)
              .WithErrorMessage("Посада занадто довга (макс. 512).");
    }

    [Fact]
    public void PositionSort_should_be_required_when_Position_provided()
    {
        var dto = new ChangePositionDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 01, 10),
            PositionSort = 0,
            Position = "Оператор",
            Note = null
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.PositionSort)
              .WithErrorMessage("Вкажіть номер посади.");
    }

    [Fact]
    public void PositionSort_should_not_error_when_Position_provided_and_sort_is_valid()
    {
        var dto = new ChangePositionDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 01, 10),
            PositionSort = 1,
            Position = "Оператор",
            Note = null
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.PositionSort);
        result.ShouldNotHaveValidationErrorFor(x => x.Position);
    }

    [Fact]
    public void Note_should_not_exceed_512_when_provided()
    {
        var dto = new ChangePositionDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 01, 10),
            PositionSort = null,
            Position = null,
            Note = new string('N', 513)
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Note)
              .WithErrorMessage("Замітка занадто довга (макс. 512).");
    }

    [Fact]
    public void Note_whitespace_should_not_trigger_max_length_rule()
    {
        var dto = new ChangePositionDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 01, 10),
            PositionSort = null,
            Position = null,
            Note = "   "
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Note);
    }

    [Fact]
    public void Valid_dto_with_position_should_have_no_errors()
    {
        var dto = new ChangePositionDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 01, 10),
            PositionSort = 5,
            Position = "Командир відділення",
            Note = "Оновлено наказом №2"
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Valid_dto_without_position_should_have_no_errors()
    {
        var dto = new ChangePositionDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 01, 10),
            PositionSort = null,
            Position = null,
            Note = null
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }
}