/*//-----------------------------------------------------------------------------
// Components/Pages/Persons/Modals/PersonCreateModal.razor.cs
//-----------------------------------------------------------------------------
// CreatePersonViewModelValidator
//-----------------------------------------------------------------------------

using FluentValidation;

namespace eRaven.Components.Pages.Persons.Modals;

public sealed class CreatePersonViewModelValidator : AbstractValidator<CreatePersonViewModel>
{
    public CreatePersonViewModelValidator()
    {
        // LastName
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Прізвище обовʼязкове.")
            .MinimumLength(2).WithMessage("Мінімум 2 символи.")
            .MaximumLength(128).WithMessage("Максимум 128 символів.")
            .Must(v => string.IsNullOrWhiteSpace(v) || v!.Trim().Length >= 2)
            .WithMessage("Прізвище не може складатися лише з пробілів.");

        // FirstName
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ім’я обовʼязкове.")
            .MinimumLength(2).WithMessage("Мінімум 2 символи.")
            .MaximumLength(128).WithMessage("Максимум 128 символів.")
            .Must(v => string.IsNullOrWhiteSpace(v) || v!.Trim().Length >= 2)
            .WithMessage("Ім’я не може складатися лише з пробілів.");

        // MiddleName (опційно, але не пробіли)
        RuleFor(x => x.MiddleName)
            .MaximumLength(128).WithMessage("Максимум 128 символів.")
            .Must(v => v == null || v.Trim().Length > 0)
            .WithMessage("По батькові не може складатися лише з пробілів.");

        // RNOKPP (обовʼязковий; 10 цифр)
        RuleFor(x => x.Rnokpp)
            .NotEmpty().WithMessage("РНОКПП обовʼязковий.")
            .Length(10).WithMessage("РНОКПП має містити рівно 10 цифр.")
            .Matches(@"^\d{10}$").WithMessage("РНОКПП має складатися лише з цифр.");

        // Rank (обовʼязковий)
        RuleFor(x => x.Rank)
            .NotEmpty().WithMessage("Звання обовʼязкове.")
            .MaximumLength(64).WithMessage("Максимум 64 символи.")
            .Must(v => !string.IsNullOrWhiteSpace(v) && v.Trim().Length > 0)
            .WithMessage("Звання не може складатися лише з пробілів.");

        // BZVP (обовʼязковий, довільний рядок)
        RuleFor(x => x.BZVP)
            .NotEmpty().WithMessage("БЗВП обовʼязковий.")
            .MaximumLength(128).WithMessage("Максимум 128 символів.")
            .Must(v => !string.IsNullOrWhiteSpace(v))
            .WithMessage("БЗВП не може складатися лише з пробілів.");

        // Weapon (опційно)
        RuleFor(x => x.Weapon)
            .MaximumLength(128).WithMessage("Максимум 128 символів.");

        // Callsign (опційно)
        RuleFor(x => x.Callsign)
            .MaximumLength(64).WithMessage("Максимум 64 символи.");
    }
}*/