//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// UpdatePersonalInfoValidatorTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Validations;
using FluentValidation.TestHelper;

namespace eRaven.Tests.Application.Validations;

public sealed class UpdatePersonalInfoValidatorTests
{
    private readonly UpdatePersonalInfoDtoValidator _sut = new();

    [Fact]
    public void Rnokpp_should_be_required_and_stop_on_first_error()
    {
        var dto = ValidDto();
        dto.Rnokpp = ""; // перша помилка - NotEmpty

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Rnokpp)
              .WithErrorMessage("РНОКПП обов'язковий.");

        // Cascade Stop: лише 1 помилка по Rnokpp
        Assert.Single(result.Errors, e => e.PropertyName == nameof(UpdatePersonalInfoDto.Rnokpp));
    }

    [Fact]
    public void Rnokpp_should_have_length_10_and_stop_on_length_error()
    {
        var dto = ValidDto();
        dto.Rnokpp = "123"; // не 10

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Rnokpp)
              .WithErrorMessage("РНОКПП має містити рівно 10 цифр.");

        Assert.Single(result.Errors, e => e.PropertyName == nameof(UpdatePersonalInfoDto.Rnokpp));
    }

    [Fact]
    public void Rnokpp_should_contain_only_digits()
    {
        var dto = ValidDto();
        dto.Rnokpp = "12345ABCDE"; // довжина 10, але не цифри

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Rnokpp)
              .WithErrorMessage("РНОКПП має містити лише цифри.");

        Assert.Single(result.Errors, e => e.PropertyName == nameof(UpdatePersonalInfoDto.Rnokpp));
    }

    [Fact]
    public void LastName_should_be_required_and_stop_on_first_error()
    {
        var dto = ValidDto();
        dto.LastName = "";

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.LastName)
              .WithErrorMessage("Прізвище обов'язкове.");

        Assert.Single(result.Errors, e => e.PropertyName == nameof(UpdatePersonalInfoDto.LastName));
    }

    [Fact]
    public void LastName_should_not_exceed_128()
    {
        var dto = ValidDto();
        dto.LastName = new string('L', 129);

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.LastName)
              .WithErrorMessage("Прізвище занадто довге (макс. 128).");

        Assert.Single(result.Errors, e => e.PropertyName == nameof(UpdatePersonalInfoDto.LastName));
    }

    [Fact]
    public void FirstName_should_be_required_and_stop_on_first_error()
    {
        var dto = ValidDto();
        dto.FirstName = "";

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.FirstName)
              .WithErrorMessage("Ім'я обов'язкове.");

        Assert.Single(result.Errors, e => e.PropertyName == nameof(UpdatePersonalInfoDto.FirstName));
    }

    [Fact]
    public void FirstName_should_not_exceed_128()
    {
        var dto = ValidDto();
        dto.FirstName = new string('F', 129);

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.FirstName)
              .WithErrorMessage("Ім'я занадто довге (макс. 128).");

        Assert.Single(result.Errors, e => e.PropertyName == nameof(UpdatePersonalInfoDto.FirstName));
    }

    [Fact]
    public void MiddleName_should_not_exceed_128_when_provided()
    {
        var dto = ValidDto();
        dto.MiddleName = new string('M', 129);

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.MiddleName)
              .WithErrorMessage("По батькові занадто довге (макс. 128).");
    }

    [Fact]
    public void MiddleName_whitespace_should_not_trigger_rule()
    {
        var dto = ValidDto();
        dto.MiddleName = "   ";

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.MiddleName);
    }

    [Fact]
    public void Note_should_not_exceed_512_when_provided()
    {
        var dto = ValidDto();
        dto.Note = new string('N', 513);

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Note)
              .WithErrorMessage("Замітка занадто довга (макс. 512).");
    }

    [Fact]
    public void Note_whitespace_should_not_trigger_rule()
    {
        var dto = ValidDto();
        dto.Note = "   ";

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Note);
    }

    [Fact]
    public void Valid_dto_should_have_no_errors()
    {
        var dto = ValidDto();

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static UpdatePersonalInfoDto ValidDto() => new()
    {
        PersonId = Guid.NewGuid(),
        Rnokpp = "1234567890",
        LastName = "Іванов",
        FirstName = "Іван",
        MiddleName = null,
        Note = null
    };
}
