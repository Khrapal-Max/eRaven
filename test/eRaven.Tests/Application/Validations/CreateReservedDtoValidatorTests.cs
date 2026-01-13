//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateReservedDtoValidatorTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Validations;
using FluentValidation.TestHelper;

namespace eRaven.Tests.Application.Validations;

public sealed class CreateReservedDtoValidatorTests
{
    private readonly CreateReservedDtoValidator _sut = new();

    private static CreateReservedDto Valid()
        => new()
        {
            Rnokpp = "1234567890",
            LastName = "Ivanov",
            FirstName = "Ivan",
            MiddleName = null,
            Rank = null,
            Position = null
        };

    // =========================
    // RNOKPP
    // =========================

    [Fact]
    public void Rnokpp_when_empty_should_have_error()
    {
        var model = Valid();
        model.Rnokpp = "";

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Rnokpp)
              .WithErrorMessage("РНОКПП обов'язковий.");
    }

    [Theory]
    [InlineData("1")]           // too short
    [InlineData("123456789")]   // 9
    [InlineData("12345678901")] // 11
    public void Rnokpp_when_length_not_10_should_have_error(string rnokpp)
    {
        var model = Valid();
        model.Rnokpp = rnokpp;

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Rnokpp)
              .WithErrorMessage("РНОКПП має містити рівно 10 цифр.");
    }

    [Theory]
    [InlineData("abcdefghij")]
    [InlineData("12345abc90")]
    [InlineData("12345-7890")]
    public void Rnokpp_when_not_digits_should_have_error(string rnokpp)
    {
        var model = Valid();
        model.Rnokpp = rnokpp;

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Rnokpp)
              .WithErrorMessage("РНОКПП має містити лише цифри.");
    }

    [Fact]
    public void Rnokpp_when_valid_should_not_have_error()
    {
        var model = Valid();

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Rnokpp);
    }

    // =========================
    // LastName / FirstName
    // =========================

    [Fact]
    public void LastName_when_empty_should_have_error()
    {
        var model = Valid();
        model.LastName = "   ";

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.LastName)
              .WithErrorMessage("Прізвище обов'язкове.");
    }

    [Fact]
    public void FirstName_when_empty_should_have_error()
    {
        var model = Valid();
        model.FirstName = "";

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.FirstName)
              .WithErrorMessage("Ім'я обов'язкове.");
    }

    [Fact]
    public void LastName_when_too_long_should_have_error()
    {
        var model = Valid();
        model.LastName = new string('a', 129);

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.LastName)
              .WithErrorMessage("Прізвище занадто довге (макс. 128).");
    }

    [Fact]
    public void FirstName_when_too_long_should_have_error()
    {
        var model = Valid();
        model.FirstName = new string('a', 129);

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.FirstName)
              .WithErrorMessage("Ім'я занадто довге (макс. 128).");
    }

    // =========================
    // Optional fields with When(...)
    // =========================

    [Fact]
    public void MiddleName_when_null_should_not_validate_length()
    {
        var model = Valid();
        model.MiddleName = null;

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.MiddleName);
    }

    [Fact]
    public void MiddleName_when_whitespace_should_not_validate_length()
    {
        var model = Valid();
        model.MiddleName = "   "; // When(...) should skip

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.MiddleName);
    }

    [Fact]
    public void MiddleName_when_too_long_should_have_error()
    {
        var model = Valid();
        model.MiddleName = new string('a', 129);

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.MiddleName)
              .WithErrorMessage("По батькові занадто довге (макс. 128).");
    }

    [Fact]
    public void Rank_when_null_should_not_validate_length()
    {
        var model = Valid();
        model.Rank = null;

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Rank);
    }

    [Fact]
    public void Rank_when_whitespace_should_not_validate_length()
    {
        var model = Valid();
        model.Rank = "   ";

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Rank);
    }

    [Fact]
    public void Rank_when_too_long_should_have_error()
    {
        var model = Valid();
        model.Rank = new string('a', 129);

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Rank)
              .WithErrorMessage("Звання занадто довге (макс. 128).");
    }

    [Fact]
    public void Position_when_null_should_not_validate_length()
    {
        var model = Valid();
        model.Position = null;

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Position);
    }

    [Fact]
    public void Position_when_whitespace_should_not_validate_length()
    {
        var model = Valid();
        model.Position = "   ";

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Position);
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

    [Fact]
    public void Whole_model_when_valid_should_have_no_errors()
    {
        var model = Valid();
        model.MiddleName = "Ivanovich";
        model.Rank = "солдат";
        model.Position = "стрілець";

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
