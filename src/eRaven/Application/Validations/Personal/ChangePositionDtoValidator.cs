//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangePositionDtoValidator
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;
using FluentValidation;

namespace eRaven.Application.Validations.Personal;

public sealed class ChangePositionDtoValidator : AbstractValidator<ChangePositionDto>
{
    public ChangePositionDtoValidator()
    {
        RuleFor(x => x.EffectiveDate)
           .NotEmpty().WithMessage("Вкажіть дату.");

        RuleFor(x => x.PositionSort)
           .GreaterThanOrEqualTo(1).WithMessage("Вкажіть номер посади.")
           .When(x => !string.IsNullOrWhiteSpace(x.Position));

        RuleFor(x => x.Position)
           .MaximumLength(512).WithMessage("Посада занадто довга (макс. 512).")
           .When(x => !string.IsNullOrWhiteSpace(x.Position));

        RuleFor(x => x.Note)
           .MaximumLength(512).WithMessage("Замітка занадто довга (макс. 512).")
           .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}