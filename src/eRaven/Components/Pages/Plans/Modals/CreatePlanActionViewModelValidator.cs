//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePlanActionViewModelValidator
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using FluentValidation;

namespace eRaven.Components.Pages.Plans.Modals;

public sealed class CreatePlanActionViewModelValidator : AbstractValidator<CreatePlanActionViewModel>
{
    public CreatePlanActionViewModelValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.PersonId).NotEmpty().WithMessage("Оберіть особу.");
        RuleFor(x => x.Location).NotEmpty().MaximumLength(128);
        RuleFor(x => x.GroupName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.CrewName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.EventAtUtc).Must(x => x.Kind == DateTimeKind.Utc)
            .WithMessage("Дата/час мають бути у UTC.");
    }
}
