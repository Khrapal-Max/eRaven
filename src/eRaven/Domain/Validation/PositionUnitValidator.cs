//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitValidator
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using FluentValidation;

namespace eRaven.Domain.Validation;

public sealed class PositionUnitValidator : AbstractValidator<PositionUnit>
{
    public PositionUnitValidator()
    {
        RuleFor(x => x.Number)
            .GreaterThan(0).WithMessage("Номер має бути більшим за 0.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Вкажіть код посади.")
            .MaximumLength(64).WithMessage("Код посади занадто довгий (макс. 64).");

        RuleFor(x => x.ShortName)
            .NotEmpty().WithMessage("Вкажіть назву посади.")
            .MaximumLength(128).WithMessage("Назва занадто довга (макс. 128).");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Вкажіть повну назву посади.")
            .MaximumLength(512).WithMessage("Повна назва занадто довга (макс. 512).");

        RuleFor(x => x.SpecialNumber)
            .NotEmpty().WithMessage("Вкажіть ВОС.")
            .MaximumLength(15).WithMessage("ВОС занадто довгий (макс. 15).");

        RuleFor(x => x.Rank)
            .NotEmpty().WithMessage("Вкажіть ШПК.")
            .MaximumLength(128).WithMessage("ШПК занадто довгий (макс. 128).");

        RuleFor(x => x.Tarif)
            .NotEmpty().WithMessage("Вкажіть тариф.")
            .MaximumLength(5).WithMessage("Тариф занадто довгий (макс. 5).");
    }
}