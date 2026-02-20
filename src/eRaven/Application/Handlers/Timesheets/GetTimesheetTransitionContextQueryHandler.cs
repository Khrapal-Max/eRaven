//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// GetTimesheetTransitionContextQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.Timesheets.Policy;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;
using eRaven.Infrastructure;

namespace eRaven.Application.Handlers.Timesheets;

/// <summary>
/// Формує контекст переходу табеля для UI на конкретну дату:
/// поточний код + дозволені опції переходів.
/// </summary>
public sealed class GetTimesheetTransitionContextQueryHandler(
    ITimesheetEpisodeRepository episodes,
    ITimesheetEntryRepository entries,
    ITimesheetPolicyRepository policy)
    : IQueryHandler<GetTimesheetTransitionContextQuery, TimesheetTransitionContextDto>
{
    private readonly ITimesheetEpisodeRepository _episodes = episodes;
    private readonly ITimesheetEntryRepository _entries = entries;
    private readonly ITimesheetPolicyRepository _policy = policy;

    /// <inheritdoc />
    public async Task<TimesheetTransitionContextDto> HandleAsync(
        GetTimesheetTransitionContextQuery query,
        CancellationToken ct = default)
    {
        if (query.PersonId == Guid.Empty)
            throw new InvalidOperationException("PersonId обов'язковий.");
        if (query.OnDate == default)
            throw new InvalidOperationException("OnDate обов'язковий.");

        var episode = await _episodes.GetEpisodeOnDateAsync(query.PersonId, query.OnDate, ct);

        // Нема епізоду => derived "НБ"
        if (episode is null)
        {
            return new TimesheetTransitionContextDto(
                PersonId: query.PersonId,
                OnDate: query.OnDate,
                CurrentCodeId: Guid.Empty,
                CurrentCode: TimesheetSystemCodes.NotInTimesheet,
                Options: []);
        }

        var entry = await _entries.GetActiveEntryOnDateAsync(episode.Id, query.PersonId, query.OnDate, ct);

        // Нема entry => derived "НБ"
        if (entry is null || entry.TimesheetCodeDefinition is null)
        {
            return new TimesheetTransitionContextDto(
                PersonId: query.PersonId,
                OnDate: query.OnDate,
                CurrentCodeId: Guid.Empty,
                CurrentCode: TimesheetSystemCodes.NotInTimesheet,
                Options: []);
        }

        var currentCode = (entry.TimesheetCodeDefinition.Code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(currentCode))
            throw new InvalidOperationException("Поточний код табеля не визначений.");

        // "НБ" не є подією
        if (string.Equals(currentCode, TimesheetSystemCodes.NotInTimesheet, StringComparison.OrdinalIgnoreCase))
        {
            return new TimesheetTransitionContextDto(
                PersonId: query.PersonId,
                OnDate: query.OnDate,
                CurrentCodeId: entry.TimesheetCodeDefinitionId,
                CurrentCode: currentCode,
                Options: []);
        }

        var rules = await _policy.GetAllowedTransitionsAsync(entry.TimesheetCodeDefinitionId, ct);

        // Мапимо у UI DTO
        var options = rules
            .Select(r => new TimesheetTransitionOptionDto(
                TransitionCodeId: r.ToCodeId,
                Code: (r.ToCode.Code ?? string.Empty).Trim(),
                Display: (r.ToCode.Title ?? string.Empty).Trim(),
                StartShiftDays: r.StartShiftDays))
            .ToList();

        // UX-фільтр: якщо поточний код = 100, то 100 -> 30 вручну все одно заборонено.
        // (Пишемо тут, бо тут ми маємо currentCode як string.)
        if (string.Equals(currentCode, TimesheetSystemCodes.DoesTheCombatTask, StringComparison.OrdinalIgnoreCase))
        {
            options = options
                .Where(o => !string.Equals(o.Code, TimesheetSystemCodes.ReadyToCombatTask, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return new TimesheetTransitionContextDto(
            PersonId: query.PersonId,
            OnDate: query.OnDate,
            CurrentCodeId: entry.TimesheetCodeDefinitionId,
            CurrentCode: currentCode,
            Options: options);
    }
}
