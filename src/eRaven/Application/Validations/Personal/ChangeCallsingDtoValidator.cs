//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeCallsingDtoValidator
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using FluentValidation;

namespace eRaven.Application.Validations.Personal;

public class ChangeCallsingDtoValidator : AbstractValidator<ChangeCallsingDto>
{
    public ChangeCallsingDtoValidator()
    {
        RuleFor(x => x.EffectiveDate)
           .NotEmpty().WithMessage("Вкажіть дату.");

        RuleFor(x => x.Callsign)
            .MaximumLength(128).WithMessage("Поле позивний занадто довге (макс. 128).")
            .When(x => !string.IsNullOrWhiteSpace(x.Callsign));
    }
}
