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
        RuleFor(x => x.PersonId)
            .NotEmpty().WithMessage("Оберіть особу.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Оберіть локацію.")
            .MaximumLength(128).WithMessage("Занадто довга назва локації.");

        RuleFor(x => x.GroupName)
            .NotEmpty().WithMessage("Оберіть групу.")
            .MaximumLength(128).WithMessage("Занадто довга назва групи.");

        RuleFor(x => x.CrewName)
            .NotEmpty().WithMessage("Оберіть екіпаж.")
            .MaximumLength(128).WithMessage("Занадто довга назва екіпажу.");

        RuleFor(x => x.EventAtUtc)
            .Must(x => x.Kind == DateTimeKind.Utc)
            .WithMessage("Дата/час мають бути у UTC.");
    }
}
