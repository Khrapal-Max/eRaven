//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateReservedCommandValidator
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using FluentValidation;

namespace eRaven.Application.Validations;

public sealed class CreateReservedDtoValidator : AbstractValidator<CreateReservedDto>
{
    public CreateReservedDtoValidator()
    {
        RuleFor(x => x.Rnokpp)
            .NotEmpty().WithMessage("РНОКПП обов'язковий.")
            .Length(10).WithMessage("РНОКПП має містити рівно 10 цифр.")
            .Matches(@"^\d{10}$").WithMessage("РНОКПП має містити лише цифри.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Прізвище обов'язкове.")
            .MaximumLength(128).WithMessage("Прізвище занадто довге (макс. 128).");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ім'я обов'язкове.")
            .MaximumLength(128).WithMessage("Ім'я занадто довге (макс. 128).");

        RuleFor(x => x.MiddleName)
            .MaximumLength(128).WithMessage("По батькові занадто довге (макс. 128).")
            .When(x => !string.IsNullOrWhiteSpace(x.MiddleName));

        RuleFor(x => x.Rank)
            .MaximumLength(128).WithMessage("Звання занадто довге (макс. 128).")
            .When(x => !string.IsNullOrWhiteSpace(x.Rank));

        RuleFor(x => x.Position)
            .MaximumLength(512).WithMessage("Посада занадто довга (макс. 512).")
            .When(x => !string.IsNullOrWhiteSpace(x.Position));

        RuleFor(x => x.Bzvp)
            .MaximumLength(128).WithMessage("БЗВП занадто довге (макс. 128).")
            .When(x => !string.IsNullOrWhiteSpace(x.Bzvp));

        RuleFor(x => x.Weapon)
            .MaximumLength(128).WithMessage("Зброя занадто довга (макс. 128).")
            .When(x => !string.IsNullOrWhiteSpace(x.Weapon));

        RuleFor(x => x.Callsign)
            .MaximumLength(128).WithMessage("Позивний занадто довгий (макс. 128).")
            .When(x => !string.IsNullOrWhiteSpace(x.Callsign));
    }
}
