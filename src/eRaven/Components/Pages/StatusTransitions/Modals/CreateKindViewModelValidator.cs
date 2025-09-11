//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// NewKindViewModelValidator
//-----------------------------------------------------------------------------

using eRaven.Application.Services.StatusKindService;
using eRaven.Application.ViewModels.StatusKindViewModels;
using FluentValidation;

namespace eRaven.Components.Pages.StatusTransitions.Modals;

public sealed class CreateKindViewModelValidator : AbstractValidator<CreateKindViewModel>
{
    public CreateKindViewModelValidator(IStatusKindService kindService)
    {
        // Name
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва обовʼязкова.")
            .MinimumLength(2).WithMessage("Мінімум 2 символи.")
            .MaximumLength(128).WithMessage("Максимум 128 символів.")
            .Must(x => !string.IsNullOrWhiteSpace(x?.Trim()))
                .WithMessage("Назва не може складатися лише з пробілів.")
            .DependentRules(() =>
            {
                When(x => !string.IsNullOrWhiteSpace(x.Name), () =>
                {
                    RuleFor(x => x.Name!)
                        .MustAsync(async (name, ct) =>
                        {
                            var trimmed = name.Trim();
                            // сервіс має повернути true, якщо ТАКИЙ вже існує
                            var exists = await kindService.NameExistsAsync(trimmed, ct);
                            return !exists; // валідно лише якщо НЕ існує
                        })
                        .WithMessage("Статус з такою назвою вже існує.");
                });
            });

        // Code
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Код обовʼязковий.")
            .MaximumLength(16).WithMessage("Код занадто довгий (до 16).")
            .Must(x => !string.IsNullOrWhiteSpace(x?.Trim()))
                .WithMessage("Код не може складатися лише з пробілів.")
            .DependentRules(() =>
            {
                When(x => !string.IsNullOrWhiteSpace(x.Code), () =>
                {
                    RuleFor(x => x.Code!)
                        .MustAsync(async (code, ct) =>
                        {
                            var trimmed = code.Trim();
                            var exists = await kindService.CodeExistsAsync(trimmed, ct);
                            return !exists;
                        })
                        .WithMessage("Код вже використовується.");
                });
            });

        // Order
        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Порядок не може бути відʼємним.");
    }
}
