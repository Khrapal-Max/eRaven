//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SetPersonStatusViewModelValidator
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PersonStatusViewModels;
using FluentValidation;

namespace eRaven.Components.Pages.Statuses.Modals
{
    public sealed class SetPersonStatusViewModelValidator : AbstractValidator<SetPersonStatusViewModel>
    {
        public SetPersonStatusViewModelValidator()
        {
            RuleFor(x => x.PersonId)
                .NotEmpty().WithMessage("Особа обов'язкова.");

            RuleFor(x => x.StatusId)
                .GreaterThan(0).WithMessage("Статус обов'язковий.");

            RuleFor(x => x.Moment)
                .NotEmpty().WithMessage("Дата та час обов'язкові.")
                .Must(m => m != default).WithMessage("Некоректна дата/час.")
                .Must(m => m.Year is >= 2000 and <= 2100)
                .WithMessage("Дата виходить за допустимий діапазон (2000–2100).");

            RuleFor(x => x.Note)
                .MaximumLength(512).WithMessage("Нотатка занадто довга (до 512).")
                // null дозволяємо; але якщо є значення — не лише пробіли
                .Must(v => v is null || !string.IsNullOrWhiteSpace(v))
                .WithMessage("Нотатка не може складатися лише з пробілів.");

            RuleFor(x => x.Author)
                .MaximumLength(64).WithMessage("Автор занадто довгий (до 64).")
                .Must(v => v is null || !string.IsNullOrWhiteSpace(v))
                .WithMessage("Автор не може складатися лише з пробілів.");
        }
    }
}