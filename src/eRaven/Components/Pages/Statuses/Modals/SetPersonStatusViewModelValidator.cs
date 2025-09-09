//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SetPersonStatusViewModelValidator
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PersonStatusViewModel;
using FluentValidation;

namespace eRaven.Components.Pages.Statuses.Modals
{
    public sealed class SetPersonStatusViewModelValidator : AbstractValidator<SetPersonStatusViewModel>
    {
        public SetPersonStatusViewModelValidator()
        {
            // PersonId
            RuleFor(x => x.PersonId)
                .NotEmpty().WithMessage("Особа обов'язкова.");

            // StatusId
            RuleFor(x => x.StatusId)
                .GreaterThan(0).WithMessage("Статус обов'язковий.");

            // Moment (локальний із UI; бек нормалізує в UTC)
            RuleFor(x => x.Moment)
                .NotEmpty().WithMessage("Дата та час обов'язкові.")
                .Must(m => m != default)
                .WithMessage("Некоректна дата/час.")
                // Обмежимо штуки з далеким майбутнім/минулим як базову гігієну
                .Must(m => m.Year is >= 2000 and <= 2100)
                .WithMessage("Дата виходить за допустимий діапазон (2000–2100).");

            // Note
            RuleFor(x => x.Note)
                .MaximumLength(512).WithMessage("Нотатка занадто довга (до 512).")
                .Must(v => string.IsNullOrWhiteSpace(v) || !string.IsNullOrWhiteSpace(v.Trim()))
                .WithMessage("Нотатка не може складатися лише з пробілів.");

            // Author
            RuleFor(x => x.Author)
                .MaximumLength(64).WithMessage("Автор занадто довгий (до 64).")
                .Must(v => string.IsNullOrWhiteSpace(v) || !string.IsNullOrWhiteSpace(v.Trim()))
                .WithMessage("Автор не може складатися лише з пробілів.");
        }
    }
}

