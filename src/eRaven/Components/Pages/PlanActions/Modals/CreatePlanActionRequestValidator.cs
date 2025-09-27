//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePlanActionRequestValidator
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using FluentValidation;

namespace eRaven.Components.Pages.PlanActions.Modals;

public sealed class CreatePlanActionRequestValidator : AbstractValidator<PlanAction>
{
    public CreatePlanActionRequestValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty().WithMessage("Особа обов'язкова.");

        RuleFor(x => x.PlanActionName)
           .MaximumLength(128).WithMessage("Назва рапорта обов'язкова.")
           .Must(v => v is null || !string.IsNullOrWhiteSpace(v))
           .WithMessage("Назва рапорта не може складатися лише з пробілів.");

        RuleFor(x => x.EffectiveAtUtc)
            .NotEmpty().WithMessage("Дата та час обов'язкові.")
            .Must(m => m != default).WithMessage("Некоректна дата/час.")
            .Must(m => m.Kind == DateTimeKind.Utc).WithMessage("Дата/час має бути в UTC.")
            .Must(m => m.Year is >= 2000 and <= 2100)
            .WithMessage("Дата виходить за допустимий діапазон (2000–2100).");

        RuleFor(x => x.ToStatusKindId)
            .GreaterThan(0).WithMessage("Статус (ToStatusKindId) обов'язковий.");

        RuleFor(x => x.MoveType)
            .IsInEnum().WithMessage("Невірний MoveType.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Локація обов'язкова.")
            .MaximumLength(256).WithMessage("Локація занадто довга (до 256).")
            .Must(v => !string.IsNullOrWhiteSpace(v!))
            .WithMessage("Локація не може складатися лише з пробілів.");

        RuleFor(x => x.GroupName)
            .MaximumLength(128).WithMessage("Група занадто довга (до 128).")
            .Must(v => v is null || !string.IsNullOrWhiteSpace(v))
            .WithMessage("Група не може складатися лише з пробілів.");

        RuleFor(x => x.CrewName)
            .MaximumLength(128).WithMessage("Екіпаж занадто довгий (до 128).")
            .Must(v => v is null || !string.IsNullOrWhiteSpace(v))
            .WithMessage("Екіпаж не може складатися лише з пробілів.");

        RuleFor(x => x.Note)
            .MaximumLength(512).WithMessage("Нотатка занадто довга (до 512).")
            .Must(v => v is null || !string.IsNullOrWhiteSpace(v))
            .WithMessage("Нотатка не може складатися лише з пробілів.");

        // --- Snapshot поля (згідно конфіга) ---
        RuleFor(x => x.Rnokpp)
            .NotEmpty().WithMessage("РНОКПП обов'язковий.")
            .MaximumLength(16).WithMessage("РНОКПП занадто довгий (до 16).")
            .Must(v => !string.IsNullOrWhiteSpace(v!))
            .WithMessage("РНОКПП не може складатися лише з пробілів.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("ПІБ обов'язкове.")
            .MaximumLength(256).WithMessage("ПІБ занадто довге (до 256).")
            .Must(v => !string.IsNullOrWhiteSpace(v!))
            .WithMessage("ПІБ не може складатися лише з пробілів.");

        RuleFor(x => x.RankName)
            .MaximumLength(64).WithMessage("Звання занадто довге (до 64).");

        RuleFor(x => x.PositionName)
            .MaximumLength(128).WithMessage("Посада занадто довга (до 128).");

        RuleFor(x => x.BZVP)
            .MaximumLength(128).WithMessage("БЗВП занадто довге (до 128).");

        RuleFor(x => x.Weapon)
            .MaximumLength(128).WithMessage("Зброя занадто довга (до 128).");

        RuleFor(x => x.Callsign)
            .MaximumLength(128).WithMessage("Позивний занадто довгий (до 128).");

        RuleFor(x => x.StatusKindOnDate)
            .MaximumLength(64).WithMessage("StatusKindOnDate занадто довге (до 64).");
    }
}
