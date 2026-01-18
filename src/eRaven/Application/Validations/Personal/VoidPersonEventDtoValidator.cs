//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// VoidPersonalEventDtoValidator
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;
using FluentValidation;

namespace eRaven.Application.Validations.Personal;

public class VoidPersonEventDtoValidator : AbstractValidator<VoidPersonEventDto>
{
    public VoidPersonEventDtoValidator()
    {
        RuleFor(x => x.TargetEventId)
            .NotEmpty().WithMessage("Вкажіть подію для корекції.");

        RuleFor(x => x.Reason)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Вкажіть причину корекції.")
            .MaximumLength(512).WithMessage("Причина занадто довга (макс. 512).");
    }
}

