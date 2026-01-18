//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeWeaponDtoValidatorTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;
using eRaven.Application.Validations.Personal;
using FluentValidation.TestHelper;

namespace eRaven.Tests.Application.Validations.Personal;

public sealed class ChangeWeaponDtoValidatorTests
{
    private readonly ChangeWeaponDtoValidator _sut = new();

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
    public void Weapon_should_allow_null_or_empty()
    {
        var dto = Valid();
        dto.Weapon = null;

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Weapon);
    }

    [Fact]
    public void Weapon_should_not_exceed_128_when_provided()
    {
        var dto = Valid();
        dto.Weapon = new string('W', 129);

        var result = _sut.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Weapon)
              .WithErrorMessage("Поле Зброя занадто довге (макс. 128).");
    }

    [Fact]
    public void Weapon_whitespace_should_not_trigger_max_length_rule()
    {
        var dto = Valid();
        dto.Weapon = "   ";

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Weapon);
    }

    [Fact]
    public void Valid_dto_should_have_no_errors()
    {
        var dto = Valid();
        dto.Weapon = "АК-74";

        var result = _sut.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static ChangeWeaponDto Valid() => new()
    {
        PersonId = Guid.NewGuid(),
        EffectiveDate = new DateOnly(2026, 01, 10),
        Weapon = null
    };
}
