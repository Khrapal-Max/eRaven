//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetPolicyForCodeOptionQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Infrastructure;

namespace eRaven.Application.Handlers.Timesheet;

/// <summary>
/// Повертає дозволені переходи (опції) для заданого коду табеля.
/// 
/// Правила:
/// <list type="bullet">
/// <item><description><c>30</c> (готовність) — дозволено вручну, якщо політика дозволяє.</description></item>
/// <item><description><c>100</c> — ніколи не пропонуємо вручну (лише документ).</description></item>
/// <item><description>З <c>100</c> повернення в <c>30</c> виконує документ (ручну опцію прибираємо).</description></item>
/// </list>
/// </summary>
public sealed class GetTimesheetPolicyForCodeOptionQueryHandler(
    ITimesheetPolicyRepository repo)
    : IQueryHandler<GetTimesheetPolicyForCodeQuery, IReadOnlyList<TimesheetTransitionOptionDto>>
{
    private readonly ITimesheetPolicyRepository _repo = repo;

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetTransitionOptionDto>> HandleAsync(
        GetTimesheetPolicyForCodeQuery query,
        CancellationToken ct = default)
    {
        var codes = await _repo.GetCodesAsync(includeInactive: true, ct);

        var taskCodeId = codes.FirstOrDefault(x =>
                string.Equals((x.Code ?? string.Empty).Trim(), TimesheetSystemCodes.DoesTheCombatTask, StringComparison.OrdinalIgnoreCase))
            ?.Id ?? throw new InvalidOperationException("Не знайдено системний код '100'.");

        var readyCodeId = codes.FirstOrDefault(x =>
                string.Equals((x.Code ?? string.Empty).Trim(), TimesheetSystemCodes.ReadyToCombatTask, StringComparison.OrdinalIgnoreCase))
            ?.Id ?? throw new InvalidOperationException("Не знайдено системний код '30'.");

        // NB може бути відсутній як definition, але якщо є — прибираємо.
        var nbCodeId = codes.FirstOrDefault(x =>
                string.Equals((x.Code ?? string.Empty).Trim(), TimesheetSystemCodes.NotInTimesheet, StringComparison.OrdinalIgnoreCase))
            ?.Id;

        var allowed = await _repo.GetAllowedTransitionsAsync(query.CodeId, ct);

        var isFromTask = query.CodeId == taskCodeId;

        return [.. allowed
            .Where(x => x.ToCode.IsActive)
            .Where(x =>
            {
                // 100 не пропонуємо вручну
                if (x.ToCodeId == taskCodeId)
                    return false;

                // NB не пропонуємо (derived)
                if (nbCodeId.HasValue && x.ToCodeId == nbCodeId.Value)
                    return false;

                // 100 -> 30 лише документ
                if (isFromTask && x.ToCodeId == readyCodeId)
                    return false;

                return true;
            })
            .OrderBy(x => x.ToCode.SortOrder)
            .ThenBy(x => x.ToCode.Priority)
            .ThenBy(x => x.ToCode.Code)
            .Select(x => new TimesheetTransitionOptionDto(
                TransitionCodeId: x.ToCodeId,
                Code: (x.ToCode.Code ?? string.Empty).Trim(),
                Display: x.ToCode.Title ?? string.Empty,
                StartShiftDays: x.StartShiftDays))];
    }
}
