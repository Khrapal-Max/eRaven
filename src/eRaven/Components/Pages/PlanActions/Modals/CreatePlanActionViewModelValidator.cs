//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePlanActionViewModelValidator
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanActionViewModels;
using eRaven.Domain.Enums;
using FluentValidation;

namespace eRaven.Components.Pages.PlanActions.Modals;

public sealed class CreatePlanActionViewModelValidator : AbstractValidator<CreatePlanActionViewModel>
{
    public CreatePlanActionViewModelValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty().WithMessage("Особа обов'язкова.");

        RuleFor(x => x.ToStatusKindId)
            .GreaterThan(0).WithMessage("Цільовий статус обов'язковий.");

        RuleFor(x => x.EffectiveAtUtc)
            .Must(d => d != default)
            .WithMessage("Необхідно вказати час.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Локація обов'язкова.")
            .MaximumLength(256);

        RuleFor(x => x.GroupName)
            .MaximumLength(128);

        RuleFor(x => x.CrewName)
            .MaximumLength(128);

        RuleFor(x => x.Note)
            .MaximumLength(512);

        When(x => x.MoveType == MoveType.Return, () =>
        {
            RuleFor(x => x.TripId)
                .NotNull().WithMessage("Для повернення необхідний TripId.");
        });
    }
}