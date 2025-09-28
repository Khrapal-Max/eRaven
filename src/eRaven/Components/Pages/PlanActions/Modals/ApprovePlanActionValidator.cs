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

        RuleFor(x => x.PersonId)
            .NotEmpty().WithMessage("PersonId обов'язковий.");

        RuleFor(x => x.EffectiveAtUtc)
            .NotEmpty().WithMessage("Дата та час обов'язкові.")
            .Must(d => d != default).WithMessage("Некоректна дата/час.")
            .Must(d => d.Kind == DateTimeKind.Utc).WithMessage("Дата/час має бути в UTC.")
            .Must(d => d.Year is >= 2000 and <= 2100)
            .WithMessage("Дата виходить за допустимий діапазон (2000–2100).");

        RuleFor(x => x.Order)
            .NotEmpty().WithMessage("Номер наказу обов'язковий.")
            .Must(v => !string.IsNullOrWhiteSpace(v))
            .WithMessage("Номер наказу не може складатися лише з пробілів.")
            .MaximumLength(512).WithMessage("Номер наказу занадто довгий (до 512).");
    }
}