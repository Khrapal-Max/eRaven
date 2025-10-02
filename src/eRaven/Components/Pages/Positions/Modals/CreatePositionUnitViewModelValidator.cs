/*//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePositionUnitViewModelValidator
//-----------------------------------------------------------------------------

using FluentValidation;

namespace eRaven.Components.Pages.Positions.Modals;

public sealed class CreatePositionUnitViewModelValidator : AbstractValidator<CreatePositionUnitViewModel>
{
    public CreatePositionUnitViewModelValidator(IPositionService positionService)
    {
        // Code
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Код обов'язковй.")
            .MaximumLength(64).WithMessage("Код занадто довгий (до 64).")
            .Must(x => string.IsNullOrWhiteSpace(x) || !string.IsNullOrWhiteSpace(x.Trim()))
            .WithMessage("Код не може складатися лише з пробілів.")
            .DependentRules(() =>
            {
                When(x => !string.IsNullOrWhiteSpace(x.Code), () =>
                {
                    RuleFor(x => x.Code!)
                        .MustAsync(async (code, ct) =>
                        {
                            var trimmed = code.Trim();
                            return !await positionService.CodeExistsActiveAsync(trimmed, ct);
                        })
                        .WithMessage("Активна посада з таким кодом вже існує.");
                });
            });

        // ShortName
        RuleFor(x => x.ShortName)
             .NotEmpty().WithMessage("Коротка назва обов'язкова.")
             .MinimumLength(2).WithMessage("Мінімум 2 символи.")
             .MaximumLength(128).WithMessage("Максимум 128 символів.");

        // SpecialNumber
        RuleFor(x => x.SpecialNumber)
            .NotEmpty().WithMessage("ВОС обов'язковй.")
            .MaximumLength(15).WithMessage("ВОС занадто довгий (до 15).");

        // OrgPath
        RuleFor(x => x.OrgPath)
            .NotEmpty().WithMessage("Шлях обов'язковий.")
            .MaximumLength(512).WithMessage("Шлях занадто довгий (до 512).");
    }
}*/