//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcludeDtoValidator
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using FluentValidation;

namespace eRaven.Application.Validations;

public sealed class ExcludeDtoValidator : AbstractValidator<ExcludeDto>
{
    public ExcludeDtoValidator()
    {
        RuleFor(x => x.EffectiveDate)
            .NotEmpty().WithMessage("Вкажіть дату виключення.");

        RuleFor(x => x.Reason)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Вкажіть підставу.")
            .MaximumLength(512).WithMessage("Підстава занадто довга (макс. 512).");
    }
}
