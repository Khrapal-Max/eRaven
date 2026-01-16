//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeBzvpDtoValidatorTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Validations.Personal;
using FluentValidation.TestHelper;

namespace eRaven.Tests.Application.Validations.Personal;

public sealed class ChangeBzvpDtoValidatorTests
{
    private readonly ChangeBzvpDtoValidator _sut = new();

    [Fact]
    public void EffectiveDate_should_be_required()
    {
        var dto = Valid();
        dto.EffectiveDate = default;

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.EffectiveDate)
              .WithErrorMessage("Вкажіть дату.");
    }

    [Fact]
    public void Bzvp_should_not_be_empty()
    {
        var dto = Valid();
        dto.Bzvp = "";

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Bzvp)
              .WithErrorMessage("Поле БЗВП не може бути порожнім.");
    }

    [Fact]
    public void Bzvp_should_not_exceed_128_chars()
    {
        var dto = Valid();
        dto.Bzvp = new string('B', 129);

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Bzvp)
              .WithErrorMessage("Поле БЗВП занадто довге (макс. 128).");
    }

    [Fact]
    public void Note_should_not_exceed_512_when_provided()
    {
        var dto = Valid();
        dto.Note = new string('N', 513);

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Note)
              .WithErrorMessage("Замітка занадто довга (макс. 512).");
    }

    [Fact]
    public void Note_whitespace_should_not_trigger_max_length_rule()
    {
        var dto = Valid();
        dto.Note = "   ";

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Note);
    }

    [Fact]
    public void Valid_dto_should_have_no_errors()
    {
        var dto = Valid();

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static ChangeBzvpDto Valid() => new()
    {
        PersonId = Guid.NewGuid(),
        EffectiveDate = new DateOnly(2026, 01, 10),
        Bzvp = "КМБ-2026",
        Note = null
    };
}
