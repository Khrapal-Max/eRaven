//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ApprovePlanActionValidator
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanActionViewModels;
using FluentValidation;

namespace eRaven.Components.Pages.PlanActions.Modals;

public sealed class ApprovePlanActionViewModelValidator : AbstractValidator<ApprovePlanActionViewModel>
{
    public ApprovePlanActionViewModelValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id обов'язковий.");

        RuleFor(x => x.Order)
            .NotEmpty().WithMessage("Номер наказу обов'язковий.")
            .MaximumLength(512).WithMessage("Номер наказу занадто довгий (до 512).");
    }
}