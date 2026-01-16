//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateReservedCommandValidator
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using FluentValidation;

namespace eRaven.Application.Validations.Personal;

public sealed class ChangeRankDtoValidator : AbstractValidator<ChangeRankDto>
{
    public ChangeRankDtoValidator()
    {
        RuleFor(x => x.EffectiveDate)
           .NotEmpty().WithMessage("Вкажіть дату.");

        RuleFor(x => x.Rank)
            .NotEmpty().WithMessage("Звання не може бути порожнім.")
            .MaximumLength(128).WithMessage("Звання занадто довге (макс. 128).");

        RuleFor(x => x.Note)
            .MaximumLength(512).WithMessage("Замітка занадто довга (макс. 512).")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
