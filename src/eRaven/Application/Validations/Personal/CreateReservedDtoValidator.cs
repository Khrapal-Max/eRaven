//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateReservedCommandValidator
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;
using FluentValidation;

namespace eRaven.Application.Validations.Personal;

public sealed class CreateReservedDtoValidator : AbstractValidator<CreateReservedDto>
{
    public CreateReservedDtoValidator()
    {
        RuleFor(x => x.Rnokpp)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("РНОКПП обов'язковий.")
            .Length(10).WithMessage("РНОКПП має містити рівно 10 цифр.")
            .Matches(@"^\d{10}$").WithMessage("РНОКПП має містити лише цифри.");

        RuleFor(x => x.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Прізвище обов'язкове.")
            .MaximumLength(128).WithMessage("Прізвище занадто довге (макс. 128).");

        RuleFor(x => x.FirstName)
            .Cascade(CascadeMode.Stop)
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
    }
}
