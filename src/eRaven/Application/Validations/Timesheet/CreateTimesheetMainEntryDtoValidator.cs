//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateTimesheetMainEntryDtoValidator
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using FluentValidation;

namespace eRaven.Application.Validations.Timesheet;

public sealed class CreateTimesheetMainEntryDtoValidator : AbstractValidator<CreateTimesheetMainEntryDto>
{
    public CreateTimesheetMainEntryDtoValidator()
    {
        RuleFor(x => x.PersonId).NotEmpty();

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Reference)
            .MaximumLength(128);

        RuleFor(x => x.Note)
            .MaximumLength(512);

        RuleFor(x => x)
            .Must(x => x.To is null || x.To.Value >= x.From)
            .WithMessage("Дата 'По' повинна бути >= дати 'З'.");
    }
}
