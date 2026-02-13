//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TransitionTimesheetStateCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheet;
using eRaven.Domain.Entities;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Infrastructure.Repositories.TimesheetRepository;

namespace eRaven.Application.Handlers.Timesheet;

public sealed class TransitionTimesheetStateCommandHandler(
    ITimesheetTimelineRepository timelines,
    ITimesheetEntryRepository entries,
    ITimesheetPolicyRepository policy)
    : ICommandHandler<TransitionTimesheetStateCommand, Guid>
{
    private readonly ITimesheetTimelineRepository _timelines = timelines;
    private readonly ITimesheetEntryRepository _entries = entries;
    private readonly ITimesheetPolicyRepository _policy = policy;

    public async Task<Guid> HandleAsync(TransitionTimesheetStateCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.PersonId == Guid.Empty)
            throw new InvalidOperationException("PersonId обов'язковий.");

        if (command.AnchorDate == default)
            throw new InvalidOperationException("AnchorDate обов'язковий.");

        if (command.InputDate == default)
            throw new InvalidOperationException("InputDate (дата події) обов'язкова.");

        if (string.IsNullOrWhiteSpace(command.NextCode))
            throw new InvalidOperationException("Код події обов'язковий.");

        var nextCode = command.NextCode.Trim();

        // 1) Таймлайн (місяць) має бути відкритим
        var timeline = await _timelines.GetTimelineOnDateAsync(command.PersonId, command.AnchorDate, ct)
            ?? throw new InvalidOperationException("Табель за обраний період не знайдено.");

        if (timeline.ClosedAt is not null)
            throw new InvalidOperationException("Табель закритий. Зміни заборонені.");

        if (command.InputDate < timeline.OpenedAt)
            throw new InvalidOperationException(
                $"Дата події {command.InputDate:yyyy-MM-dd} раніше відкриття табеля {timeline.OpenedAt:yyyy-MM-dd}.");

        if (command.InputDate < command.AnchorDate)
            throw new InvalidOperationException("Дата події не може бути раніше поточного дня.");

        // 2) Поточний активний запис на AnchorDate
        var prevEntry = await _entries.GetActiveEntryOnDateAsync(timeline.Id, command.PersonId, command.AnchorDate, ct)
            ?? throw new InvalidOperationException("Не знайдено активний запис на обрану дату.");

        if (prevEntry.TimesheetCodeDefinition is null)
            throw new InvalidOperationException("Не визначено код табеля (TimesheetCodeDefinition).");

        var prevCode = (prevEntry.TimesheetCodeDefinition.Code ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(prevCode))
            throw new InvalidOperationException("Поточний код табеля не визначений.");

        if (string.Equals(prevCode, TimesheetSystemCodes.NotInTimesheet, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Перехід із “НБ” неможливий (це не подія табеля).");

        // 3) Підтягуємо довідник кодів (includeInactive=true, щоб не ламати історію)
        var allCodes = await _policy.GetCodesAsync(includeInactive: true, ct);

        var prevDef = allCodes.FirstOrDefault(x => x.Id == prevEntry.TimesheetCodeDefinitionId)
            ?? throw new InvalidOperationException($"Для поточного коду немає запису у політиці.");

        var nextDef = allCodes.FirstOrDefault(x =>
            string.Equals(x.Code?.Trim(), nextCode, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Код “{nextCode}” не знайдено у політиці.");

        // 4) Правило переходу (From -> To) + StartShiftDays
        var allowed = await _policy.GetAllowedTransitionsAsync(prevDef.Id, ct);

        var rule = allowed.FirstOrDefault(x => x.ToCodeId == nextDef.Id) 
            ?? throw new InvalidOperationException($"Перехід “{prevDef.Code} → {nextDef.Code}” заборонений політикою.");
        
        var shift = rule.StartShiftDays;
        if (shift < 0)
            throw new InvalidOperationException("Некоректне правило політики: StartShiftDays < 0.");

        // InputDate = "дата події" (введена користувачем)
        // shift=0 => новий код активний цього ж дня
        // shift=1 => цей день ще старий код, новий з наступного дня
        var nextFrom = command.InputDate.AddDays(shift);
        var prevTo = nextFrom.AddDays(-1);

        if (!command.IsCorrection)
        {
            var nextExisting = await _entries.GetNextEntryAfterDateAsync(
                timeline.Id, command.PersonId, nextFrom.AddDays(-1), ct);

            if (nextExisting is not null)
                throw new InvalidOperationException(
                    $"Є наступна подія: {nextExisting.From:yyyy-MM-dd} ({nextExisting.TimesheetCodeDefinition?.Code}). Використайте режим 'Корекція'.");
        }

        // 5) Не можна закривати попередній стан раніше його старту.
        //    Але дозволяємо "replace", якщо новий стартує рівно з prev.From.
        if (prevTo < prevEntry.From)
        {
            if (nextFrom != prevEntry.From)
                throw new InvalidOperationException("Дата події некоректна: вона закриває поточний стан раніше його початку.");

            prevEntry.TimesheetCodeDefinitionId = nextDef.Id;
            prevEntry.Reference = string.IsNullOrWhiteSpace(command.Reference) ? null : command.Reference.Trim();
            prevEntry.Note = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim();
            prevEntry.UpdatedBy = command.Author;
            prevEntry.UpdatedAtUtc = command.NowUtc;

            await _entries.UpdateAsync(prevEntry, ct);
            return prevEntry.Id;
        }

        // 6) Стандартний перехід: закриваємо prev і створюємо next
        prevEntry.To = prevTo;
        prevEntry.UpdatedBy = command.Author;
        prevEntry.UpdatedAtUtc = command.NowUtc;

        var nextEntry = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimelineId = timeline.Id,
            PersonId = command.PersonId,
            TimesheetCodeDefinitionId = nextDef.Id,
            From = nextFrom,
            To = null,
            Reference = string.IsNullOrWhiteSpace(command.Reference) ? null : command.Reference.Trim(),
            Note = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim(),
            CreatedBy = command.Author,
            CreatedAtUtc = command.NowUtc
        };

        await _entries.SaveTransitionAsync(prevEntry, nextEntry, ct);
        return nextEntry.Id;
    }
}
