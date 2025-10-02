/*//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EditPersonViewModelValidator
//-----------------------------------------------------------------------------

using FluentValidation;

namespace eRaven.Components.Pages.Persons;

public sealed class EditPersonViewModelValidator : AbstractValidator<EditPersonViewModel>
{
    public EditPersonViewModelValidator()
    {
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Прізвище обовʼязкове.")
            .MinimumLength(2).WithMessage("Мінімум 2 символи.")
            .MaximumLength(128).WithMessage("Максимум 128 символів.")
            .Must(v => string.IsNullOrWhiteSpace(v) || v!.Trim().Length >= 2)
            .WithMessage("Прізвище не може складатися лише з пробілів.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ім’я обовʼязкове.")
            .MinimumLength(2).WithMessage("Мінімум 2 символи.")
            .MaximumLength(128).WithMessage("Максимум 128 символів.")
            .Must(v => string.IsNullOrWhiteSpace(v) || v!.Trim().Length >= 2)
            .WithMessage("Ім’я не може складатися лише з пробілів.");

        RuleFor(x => x.MiddleName)
            .MaximumLength(128).WithMessage("Максимум 128 символів.")
            .Must(v => string.IsNullOrEmpty(v) || v.Trim().Length > 0)
            .WithMessage("По батькові не може складатися лише з пробілів.");

        RuleFor(x => x.Rnokpp)
            .NotEmpty().WithMessage("РНОКПП обовʼязковий.")
            .Length(10).WithMessage("РНОКПП має містити рівно 10 символів.")
            .Matches(@"^\d{10}$").WithMessage("РНОКПП має складатися лише з цифр.");

        RuleFor(x => x.Rank)
            .NotEmpty().WithMessage("Звання обовʼязкове.")
            .MaximumLength(64).WithMessage("Максимум 64 символи.")
            .Must(v => !string.IsNullOrWhiteSpace(v) && v.Trim().Length > 0)
            .WithMessage("Звання не може складатися лише з пробілів.");

        RuleFor(x => x.BZVP)
            .NotEmpty().WithMessage("БЗВП обовʼязковий.")
            .MaximumLength(128).WithMessage("Максимум 128 символів.")
            .Must(v => !string.IsNullOrWhiteSpace(v))
            .WithMessage("БЗВП не може складатися лише з пробілів.");

        RuleFor(x => x.Weapon).MaximumLength(128).WithMessage("Максимум 128 символів.");
        RuleFor(x => x.Callsign).MaximumLength(64).WithMessage("Максимум 64 символи.");
    }
}*/