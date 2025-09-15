// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanActionViewModelValidator
// -----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using FluentValidation;

namespace eRaven.Components.Pages.Plans.Modals;

public sealed class PlanActionViewModelValidator : AbstractValidator<PlanActionViewModel>
{
    public PlanActionViewModelValidator()
    {
        // Номер плану
        RuleFor(x => x.PlanNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Номер плану обов’язковий.")
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("Номер плану не може бути з пробілів.")
            .MaximumLength(64).WithMessage("Номер плану не повинен перевищувати 64 символи.");

        // Особа
        RuleFor(x => x.PersonId)
            .NotEmpty().WithMessage("Потрібно обрати особу.");

        // Тип дії
        RuleFor(x => x.ActionType)
            .IsInEnum().WithMessage("Невідомий тип дії.")
            .Must(t => t == PlanActionType.Dispatch || t == PlanActionType.Return)
            .WithMessage("Дозволені дії: Відрядити/Повернути.");

        // Час (локальний — сервіс сам нормалізує до UTC)
        RuleFor(x => x.EventAtUtc)
            .Must(dt => dt.Year >= 2020 && dt.Year <= DateTime.UtcNow.AddYears(1).Year)
            .WithMessage("Некоректний час події.");

        // Локація/Група/Екіпаж — обов’язково, довжини
        RuleFor(x => x.Location)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Вкажіть локацію.")
            .Must(NotWhiteSpace).WithMessage("Локація не може складатись лише з пробілів.")
            .MaximumLength(128).WithMessage("Локація не довша за 128 символів.");

        RuleFor(x => x.GroupName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Вкажіть групу.")
            .Must(NotWhiteSpace).WithMessage("Група не може складатись лише з пробілів.")
            .MaximumLength(128).WithMessage("Група не довша за 128 символів.");

        RuleFor(x => x.CrewName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Вкажіть екіпаж.")
            .Must(NotWhiteSpace).WithMessage("Екіпаж не може складатись лише з пробілів.")
            .MaximumLength(128).WithMessage("Екіпаж не довший за 128 символів.");

        RuleFor(x => x.Note)
            .MaximumLength(512).WithMessage("Примітка не довша за 512 символів.");
    }

    private static bool NotWhiteSpace(string? s) => !string.IsNullOrWhiteSpace(s);
}