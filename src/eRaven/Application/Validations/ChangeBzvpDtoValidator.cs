//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeBzvpDtoValidator
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using FluentValidation;

namespace eRaven.Application.Validations;

public class ChangeBzvpDtoValidator : AbstractValidator<ChangeBzvpDto>
{
    public ChangeBzvpDtoValidator()
    {
        RuleFor(x => x.EffectiveDate)
           .NotEmpty().WithMessage("Вкажіть дату.");

        RuleFor(x => x.Bzvp)
            .NotEmpty().WithMessage("Поле БЗВП не може бути порожнім.")
            .MaximumLength(128).WithMessage("Поле БЗВП занадто довге (макс. 128).");

        RuleFor(x => x.Note)
            .MaximumLength(512).WithMessage("Замітка занадто довга (макс. 512).")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
