//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollDtoValidatorTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Person;
using eRaven.Application.Validations.Personal;
using FluentValidation.TestHelper;

namespace eRaven.Tests.Application.Validations.Personal;

public sealed class EnrollDtoValidatorTests
{
    private readonly EnrollDtoValidator _sut = new();

    private static EnrollDto Valid(EnrollmentKindDto kind = EnrollmentKindDto.Unit)
        => new()
        {
            Id = Guid.NewGuid(),
            Kind = kind,
            Reference = null,
            Reason = "Підстава",
            EnrollDate = new DateOnly(2026, 01, 10),
            Rank = "солдат",
            PositionSort = 1,
            Position = "стрілець"
        };

    // =========================
    // Kind
    // =========================

    [Fact]
    public void Kind_when_valid_should_not_have_error()
    {
        var model = Valid();

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Kind);
    }

    // =========================
    // Reference (optional + max 128)
    // =========================

    [Fact]
    public void Reference_when_null_should_not_have_error()
    {
        var model = Valid();
        model.Reference = null;

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Reference);
    }

    [Fact]
    public void Reference_when_whitespace_should_not_have_error()
    {
        var model = Valid();
        model.Reference = "   ";

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Reference);
    }

    [Fact]
    public void Reference_when_too_long_should_have_error()
    {
        var model = Valid();
        model.Reference = new string('a', 129);

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Reference)
              .WithErrorMessage("Посилання/номер занадто довгий (макс. 128).");
    }

    // =========================
    // Reason (required + max 512)
    // =========================

    [Fact]
    public void Reason_when_empty_should_have_error()
    {
        var model = Valid();
        model.Reason = "";

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Reason)
              .WithErrorMessage("Вкажіть підставу.");
    }

    [Fact]
    public void Reason_when_too_long_should_have_error()
    {
        var model = Valid();
        model.Reason = new string('a', 513);

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Reason)
              .WithErrorMessage("Підстава занадто довга (макс. 512).");
    }

    // =========================
    // EnrollDate (required)
    // =========================

    [Fact]
    public void EnrollDate_when_default_should_have_error()
    {
        var model = Valid();
        model.EnrollDate = default;

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.EnrollDate)
              .WithErrorMessage("Вкажіть дату зарахування.");
    }

    // =========================
    // Rank (required)
    // =========================

    [Fact]
    public void Rank_when_empty_should_have_error()
    {
        var model = Valid();
        model.Rank = "   ";

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Rank)
              .WithErrorMessage("Вкажіть звання.");
    }

    // =========================
    // PositionSort (>=1 only for Unit)
    // =========================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PositionSort_when_unit_and_less_than_1_should_have_error(int sort)
    {
        var model = Valid(EnrollmentKindDto.Unit);
        model.PositionSort = sort;

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.PositionSort)
              .WithErrorMessage("Вкажіть номер посади.");
    }

    [Fact]
    public void PositionSort_when_unit_and_ok_should_not_have_error()
    {
        var model = Valid(EnrollmentKindDto.Unit);
        model.PositionSort = 1;

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.PositionSort);
    }

    [Theory]
    [InlineData(EnrollmentKindDto.AttachedByOrder)]
    [InlineData(EnrollmentKindDto.AttachedByList)]
    public void PositionSort_when_not_unit_should_not_be_validated_even_if_zero(EnrollmentKindDto kind)
    {
        var model = Valid(kind);
        model.PositionSort = 0; // invalid for Unit, but should be ignored for non-Unit

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.PositionSort);
    }

    // =========================
    // Position (required + max 512)
    // =========================

    [Fact]
    public void Position_when_empty_should_have_error()
    {
        var model = Valid();
        model.Position = "";

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Position)
              .WithErrorMessage("Вкажіть посаду.");
    }

    [Fact]
    public void Position_when_too_long_should_have_error()
    {
        var model = Valid();
        model.Position = new string('a', 513);

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Position)
              .WithErrorMessage("Посада занадто довга (макс. 512).");
    }

    // =========================
    // Whole model
    // =========================

    [Fact]
    public void Whole_model_when_valid_unit_should_have_no_errors()
    {
        var model = Valid(EnrollmentKindDto.Unit);

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Whole_model_when_valid_attached_should_have_no_errors()
    {
        var model = Valid(EnrollmentKindDto.AttachedByOrder);
        model.PositionSort = 0; // ignored by validator (non-Unit)

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
