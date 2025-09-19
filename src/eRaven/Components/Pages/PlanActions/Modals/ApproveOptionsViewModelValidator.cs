//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ApproveOptionsViewModelValidator
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanActionViewModels;
using FluentValidation;

namespace eRaven.Components.Pages.PlanActions.Modals;

public sealed class ApproveOptionsViewModelValidator : AbstractValidator<ApproveOptionsViewModel>
{
    public ApproveOptionsViewModelValidator()
    {
        RuleFor(x => x.OrderName)
            .NotEmpty().WithMessage("Назва/номер наказу обов'язкові.")
            .MaximumLength(128);

        RuleFor(x => x.Author)
            .MaximumLength(64);

        RuleFor(x => x.SelectedActionIds)
            .NotEmpty().WithMessage("Оберіть принаймні одну дію.");
    }
}
