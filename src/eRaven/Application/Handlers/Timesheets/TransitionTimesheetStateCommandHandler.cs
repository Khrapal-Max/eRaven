//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// TransitionTimesheetStateCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheets;
using eRaven.Domain.Enums;

namespace eRaven.Application.Handlers.Timesheets;

/// <summary>
/// Виконує перехід табельного стану для особи.
/// </summary>
public sealed class TransitionTimesheetStateCommandHandler(
    ITimesheetEntryQueryRepository entries,
    ITimesheetEntryWriterRepository writer,
    ITimesheetPolicyRepository policy)
    : ICommandHandler<TransitionTimesheetStateCommand, Guid>
{
    private readonly ITimesheetEntryQueryRepository _entries = entries;
    private readonly ITimesheetEntryWriterRepository _writer = writer;
    private readonly ITimesheetPolicyRepository _policy = policy;

    public async Task<Guid> HandleAsync(TransitionTimesheetStateCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.PersonId == Guid.Empty)
            throw new InvalidOperationException("PersonId обов'язковий.");
        if (command.AnchorDate == default)
            throw new InvalidOperationException("AnchorDate обов'язковий.");
        if (command.InputDate == default)
            throw new InvalidOperationException("InputDate обов'язковий.");
        if (command.NextCode == Guid.Empty)
            throw new InvalidOperationException("NextCode обов'язковий.");
        if (string.IsNullOrWhiteSpace(command.Author))
            throw new InvalidOperationException("Author обов'язковий.");

        // UI flow: корекції виконуються в персональному табелі
        if (!command.IsCorrection && command.InputDate < command.AnchorDate)
            throw new InvalidOperationException("Дата події не може бути раніше операційної дати.");

        // 1) Поточний entry на дату події.
        // AnchorDate у UI використовується лише як "операційна дата" для контролю корекцій.
        var prev = await _entries.GetActiveEntryOnDateAsync(command.PersonId, command.InputDate, ct);
        if (prev?.TimesheetCodeDefinition is null)
            throw new InvalidOperationException("На обрану дату немає активного запису табеля (НБ — derived).");

        // 2) Перевірки ролей
        var allCodes = await _policy.GetCodesAsync(includeInactive: true, ct);
        var nextDef = allCodes.FirstOrDefault(x => x.Id == command.NextCode)
            ?? throw new InvalidOperationException("Наступний код не знайдено у політиці.");

        if (nextDef.RoleCode == RoleCode.SystemCode)
            throw new InvalidOperationException("SystemCode не можна встановлювати вручну.");

        if (prev.TimesheetCodeDefinition.RoleCode == RoleCode.SystemCode)
            throw new InvalidOperationException("Під активним system-кодом ручні події заблоковані.");

        // 3) Emergency code дозволяємо завжди (shift=0); Transition — лише по матриці
        if (nextDef.RoleCode == RoleCode.EmergencyCode)
        {
            return await _writer.TransitionAsync(
                personId: command.PersonId,
                effectiveAt: command.InputDate,
                nextCodeId: nextDef.Id,
                reference: command.Reference,
                note: command.Note,
                author: command.Author,
                nowUtc: command.NowUtc,
                ct: ct);
        }

        // 4) Matrix check
        var allowed = await _policy.GetAllowedCodesAsync(prev.TimesheetCodeDefinitionId, ct);
        var rule = allowed.FirstOrDefault(x => x.ToCodeId == nextDef.Id)
            ?? throw new InvalidOperationException($"Перехід “{prev.TimesheetCodeDefinition.Code} → {nextDef.Code}” заборонений політикою.");

        var nextFrom = command.InputDate.AddDays(rule.StartShiftDays);

        // non-correction: не вставляємо в середину, якщо вже є наступна подія
        if (!command.IsCorrection)
        {
            var nextExisting = await _entries.GetNextEntryAfterDateAsync(command.PersonId, nextFrom, ct);
            if (nextExisting is not null)
                throw new InvalidOperationException("Неможливо вставити подію: після цієї дати вже існує наступна подія. Використайте корекцію в персональному табелі.");
        }

        return await _writer.TransitionAsync(
            personId: command.PersonId,
            effectiveAt: nextFrom,
            nextCodeId: nextDef.Id,
            reference: command.Reference,
            note: command.Note,
            author: command.Author,
            nowUtc: command.NowUtc,
            ct: ct);
    }
}
