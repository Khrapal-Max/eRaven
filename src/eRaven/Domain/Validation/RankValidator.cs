//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RankValidator
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using FluentValidation;

namespace eRaven.Domain.Validation;

public class RankValidator : AbstractValidator<Rank>
{
    public RankValidator()
    {
        RuleFor(x => x.Title)
          .NotEmpty().WithMessage("Вкажіть назву звання.")
          .MaximumLength(64).WithMessage("Назва звання занадто довга (макс. 512).");

        RuleFor(x => x.Priority)
            .GreaterThan(0).WithMessage("Номер має бути більшим за 0.");
    }
}
