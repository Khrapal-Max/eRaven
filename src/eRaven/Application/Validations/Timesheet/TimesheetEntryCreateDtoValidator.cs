//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntryCreateDtoValidator
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using FluentValidation;

namespace eRaven.Application.Validations.Timesheet;

public sealed class TimesheetEntryCreateDtoValidator : AbstractValidator<TimesheetEntryCreateDto>
{
    public TimesheetEntryCreateDtoValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty().WithMessage("Особа не вибрана.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Вкажіть код.")
            .MaximumLength(32).WithMessage("Код занадто довгий (макс. 32).");

        RuleFor(x => x.From)
            .NotEmpty().WithMessage("Вкажіть дату початку.");

        RuleFor(x => x.To)
            .Must((m, to) => !to.HasValue || to.Value >= m.From)
            .WithMessage("Дата завершення має бути >= дати початку.");

        RuleFor(x => x.Reference)
            .MaximumLength(128).WithMessage("Референс занадто довгий (макс. 128).")
            .When(x => !string.IsNullOrWhiteSpace(x.Reference));

        RuleFor(x => x.Note)
            .MaximumLength(512).WithMessage("Примітка занадто довга (макс. 512).")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
