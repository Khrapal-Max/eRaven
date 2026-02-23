//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TransitionTimesheetStateCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheets;

namespace eRaven.Application.Handlers.Timesheets;

/// <summary>
/// Виконує перехід табельного стану для особи.
///
/// <para>Інваріанти:</para>
/// <list type="bullet">
/// <item><description><c>30 → 100</c> та <c>100 → 30</c> виконує лише документ (ручні переходи заборонені).</description></item>
/// <item><description><c>100</c> не можна встановлювати вручну.</description></item>
/// <item><description>Під активним завданням (код <c>100</c>) ручні події блокуються, крім виходів з <c>100</c>
/// на будь-який інший дозволений політикою код (крім <c>30</c>).</description></item>
/// </list>
/// </summary>
public sealed class TransitionTimesheetStateCommandHandler(
    ITimesheetEpisodeRepository episodes,
    ITimesheetPolicyRepository policy)
    : ICommandHandler<TransitionTimesheetStateCommand, Guid>
{
    private readonly ITimesheetEpisodeRepository _episodes = episodes;
    private readonly ITimesheetPolicyRepository _policy = policy;

    /// <inheritdoc />
    public async Task<Guid> HandleAsync(TransitionTimesheetStateCommand command, CancellationToken ct = default)
    {
        /*  ArgumentNullException.ThrowIfNull(command);

          if (command.PersonId == Guid.Empty)
              throw new InvalidOperationException("PersonId обов'язковий.");
          if (command.AnchorDate == default)
              throw new InvalidOperationException("AnchorDate обов'язковий.");
          if (command.InputDate == default)
              throw new InvalidOperationException("InputDate обов'язковий.");
          if (command.NextCode == Guid.Empty)
              throw new InvalidOperationException("NextCode (Id) обов'язковий.");
          if (string.IsNullOrWhiteSpace(command.Author))
              throw new InvalidOperationException("Author обов'язковий.");
          if (command.NowUtc == default)
              throw new InvalidOperationException("NowUtc обов'язковий.");

          // 1) Episode
          var episode = await _episodes.LoadEpisodeOnDateForUpdateAsync(command.PersonId, command.AnchorDate, ct)
              ?? throw new InvalidOperationException("Табель за обраний період не знайдено.");

          if (episode.ClosedAt is not null)
              throw new InvalidOperationException("Табель закритий. Зміни заборонені.");

          if (command.InputDate < episode.OpenedAt)
              throw new InvalidOperationException($"Дата події {command.InputDate:yyyy-MM-dd} раніше відкриття табеля {episode.OpenedAt:yyyy-MM-dd}.");

          if (command.InputDate < command.AnchorDate)
              throw new InvalidOperationException("Дата події не може бути раніше поточного дня.");

          // 2) Prev entry on anchor
          var prevEntry = await _entries.GetActiveEntryOnDateAsync(episode.Id, command.PersonId, command.AnchorDate, ct)
              ?? throw new InvalidOperationException("Не знайдено активний запис на обрану дату.");

          if (prevEntry.TimesheetCodeDefinition is null)
              throw new InvalidOperationException("Не визначено код табеля (TimesheetCodeDefinition).");

          var prevCodeString = (prevEntry.TimesheetCodeDefinition.Code ?? string.Empty).Trim();
          if (string.IsNullOrWhiteSpace(prevCodeString))
              throw new InvalidOperationException("Поточний код табеля не визначений.");

          if (string.Equals(prevCodeString, TimesheetSystemCodes.NotInTimesheet, StringComparison.OrdinalIgnoreCase))
              throw new InvalidOperationException("Перехід із “НБ” неможливий (це derived, не подія).");

          // 3) Codes catalog (one load)
          var allCodes = await _policy.GetCodesAsync(includeInactive: true, ct);

          var taskCode = allCodes.FirstOrDefault(x =>
                  string.Equals((x.Code ?? string.Empty).Trim(), TimesheetSystemCodes.DoesTheCombatTask, StringComparison.OrdinalIgnoreCase))
              ?? throw new InvalidOperationException("Не знайдено системний код '100'.");

          var readyCode = allCodes.FirstOrDefault(x =>
                  string.Equals((x.Code ?? string.Empty).Trim(), TimesheetSystemCodes.ReadyToCombatTask, StringComparison.OrdinalIgnoreCase))
              ?? throw new InvalidOperationException("Не знайдено системний код '30'.");

          var prevDef = allCodes.FirstOrDefault(x => x.Id == prevEntry.TimesheetCodeDefinitionId)
              ?? throw new InvalidOperationException("Для поточного коду немає запису у політиці.");

          var nextDef = allCodes.FirstOrDefault(x => x.Id == command.NextCode)
              ?? throw new InvalidOperationException("Наступний код (Id) не знайдено у політиці.");

          var prevIsTask = prevDef.Id == taskCode.Id;
          var nextIsTask = nextDef.Id == taskCode.Id;
          var nextIsReady = nextDef.Id == readyCode.Id;

          // 3.1) 100 не можна ставити вручну
          if (nextIsTask)
              throw new InvalidOperationException("Код “100” встановлюється лише документом бойового завдання.");

          // 3.2) 100 -> 30 лише документ
          if (prevIsTask && nextIsReady)
              throw new InvalidOperationException("Повернення з “100” до “30” виконується документом завдання.");

          // 4) Policy rule
          var allowed = await _policy.GetAllowedTransitionsAsync(prevDef.Id, ct);

          var rule = allowed.FirstOrDefault(x => x.ToCodeId == nextDef.Id)
              ?? throw new InvalidOperationException($"Перехід “{prevDef.Code} → {nextDef.Code}” заборонений політикою.");

          var shift = rule.StartShiftDays;
          if (shift < 0)
              throw new InvalidOperationException("Некоректне правило політики: StartShiftDays < 0.");

          var nextFrom = command.InputDate.AddDays(shift);
          var prevToExclusive = nextFrom;

          // 4.1) Під активним завданням:
          // - якщо поточний код не 100 -> блок
          // - якщо поточний код 100 -> дозволяємо вихід на будь-який інший дозволений політикою код (крім 30)
          var entryOnNextFrom = await _entries.GetActiveEntryOnDateAsync(episode.Id, command.PersonId, nextFrom, ct);
          var hasTaskOnNextFrom = entryOnNextFrom is not null && entryOnNextFrom.TimesheetCodeDefinitionId == taskCode.Id;

          if (hasTaskOnNextFrom && !prevIsTask)
              throw new InvalidOperationException("Є активне завдання на цю дату. Ручні події табеля заблоковані.");

          // 4.2) non-correction вставка заборонена, якщо вже є подія СТРОГО ПІЗНІШЕ nextFrom
          if (!command.IsCorrection)
          {
              var nextExisting = await _entries.GetNextEntryAfterDateAsync(
                  episode.Id,
                  command.PersonId,
                  nextFrom,
                  ct);

              if (nextExisting is not null)
                  throw new InvalidOperationException(
                      $"Є наступна подія: {nextExisting.From:yyyy-MM-dd} ({nextExisting.TimesheetCodeDefinition?.Code}). " +
                      "Використайте режим 'Корекція'.");
          }

          // 5) Replace або стандартний перехід (half-open entries)
          // prev ends at nextFrom (exclusive). If nextFrom == prev.From => replace in-place.
          if (prevToExclusive <= prevEntry.From)
          {
              if (nextFrom != prevEntry.From)
                  throw new InvalidOperationException("Дата події некоректна: вона закриває поточний стан раніше його початку.");

              prevEntry.TimesheetCodeDefinitionId = nextDef.Id;
              prevEntry.TimesheetCodeDefinition = null;

              prevEntry.Reference = string.IsNullOrWhiteSpace(command.Reference) ? null : command.Reference.Trim();
              prevEntry.Note = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim();
              prevEntry.UpdatedBy = command.Author;
              prevEntry.UpdatedAtUtc = command.NowUtc;

              await _entries.UpdateAsync(prevEntry, ct);
              return prevEntry.Id;
          }

          prevEntry.To = prevToExclusive;
          prevEntry.UpdatedBy = command.Author;
          prevEntry.UpdatedAtUtc = command.NowUtc;

          var nextEntry = new TimesheetEntry
          {
              Id = Guid.NewGuid(),
              TimesheetId = episode.Id,
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
          return nextEntry.Id;*/

        throw new NotImplementedException();
    }
}
