//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeWeaponDtoValidator
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;
using FluentValidation;

namespace eRaven.Application.Validations.Personal;

public class ChangeWeaponDtoValidator : AbstractValidator<ChangeWeaponDto>
{
    public ChangeWeaponDtoValidator()
    {
        RuleFor(x => x.EffectiveDate)
           .NotEmpty().WithMessage("Вкажіть дату.");

        RuleFor(x => x.Weapon)
            .MaximumLength(128).WithMessage("Поле Зброя занадто довге (макс. 128).")
            .When(x => !string.IsNullOrWhiteSpace(x.Weapon));
    }
}
