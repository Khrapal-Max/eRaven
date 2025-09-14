// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// CreatePlanViewModelValidator
// -----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using FluentValidation;

namespace eRaven.Components.Pages.Plans.Modals;

public sealed class CreatePlanViewModelValidator : AbstractValidator<CreatePlanViewModel>
{
    public CreatePlanViewModelValidator()
    {
        RuleFor(x => x.PlanNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Номер плану обов’язковий.")
            .Must(s => !string.IsNullOrWhiteSpace(s))
                .WithMessage("Номер плану не може складатися лише з пробілів.")
            .MaximumLength(64).WithMessage("Номер плану не повинен перевищувати 64 символи.");
    }
}