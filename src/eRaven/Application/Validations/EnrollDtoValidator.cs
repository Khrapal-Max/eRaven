//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollCommandValidator
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Domain.Enums;
using FluentValidation;

namespace eRaven.Application.Validations;

public sealed class EnrollDtoValidator : AbstractValidator<EnrollDto>
{
    public EnrollDtoValidator()
    {
        RuleFor(x => x.Kind)
            .IsInEnum().WithMessage("Вкажіть коректний тип зарахування.");

        RuleFor(x => x.Reference)
            .MaximumLength(128).WithMessage("Посилання/номер занадто довгий (макс. 128).")
            .When(x => !string.IsNullOrWhiteSpace(x.Reference));

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Вкажіть підставу.")
            .MaximumLength(512).WithMessage("Підстава занадто довга (макс. 512).");

        RuleFor(x => x.EnrollDate)
            .NotEmpty().WithMessage("Вкажіть дату зарахування.");

        RuleFor(x => x.Rank)
            .NotEmpty().WithMessage("Вкажіть звання.");

        RuleFor(x => x.PositionSort)
            .GreaterThanOrEqualTo(1).WithMessage("Вкажіть номер посади.")
            .When(x => x.Kind == EnrollmentKind.Unit); 

        RuleFor(x => x.Position)
            .NotEmpty().WithMessage("Вкажіть посаду.")
            .MaximumLength(512).WithMessage("Посада занадто довга (макс. 512).");
    }
}