//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeRankDtoValidatorTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;
using eRaven.Application.Validations.Personal;
using FluentValidation.TestHelper;

namespace eRaven.Tests.Application.Validations.Personal;

public sealed class ChangeRankDtoValidatorTests
{
    private readonly ChangeRankDtoValidator _sut = new();

    [Fact]
    public void EffectiveDate_should_be_required()
    {
        var dto = new ChangeRankDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = default, // DateOnly.MinValue => NotEmpty має впасти
            Rank = "Солдат",
            Note = null
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.EffectiveDate)
              .WithErrorMessage("Вкажіть дату.");
    }

    [Fact]
    public void Rank_should_not_be_empty()
    {
        var dto = new ChangeRankDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 01, 10),
            Rank = "",
            Note = null
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Rank)
              .WithErrorMessage("Звання не може бути порожнім.");
    }

    [Fact]
    public void Rank_should_not_exceed_128_chars()
    {
        var dto = new ChangeRankDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 01, 10),
            Rank = new string('A', 129),
            Note = null
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Rank)
              .WithErrorMessage("Звання занадто довге (макс. 128).");
    }

    [Fact]
    public void Note_should_not_exceed_512_when_provided()
    {
        var dto = new ChangeRankDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 01, 10),
            Rank = "Солдат",
            Note = new string('N', 513)
        };

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Note)
              .WithErrorMessage("Замітка занадто довга (макс. 512).");
    }

    [Fact]
    public void Note_whitespace_should_not_trigger_max_length_rule()
    {
        // важливо: правило на Note виконується тільки When(...not whitespace)
        var dto = new ChangeRankDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 01, 10),
            Rank = "Солдат",
            Note = "   " // whitespace => When(...) false => помилки бути не повинно
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Note);
    }

    [Fact]
    public void Valid_dto_should_have_no_errors()
    {
        var dto = new ChangeRankDto
        {
            PersonId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 01, 10),
            Rank = "Сержант",
            Note = "Оновлено наказом №1"
        };

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
