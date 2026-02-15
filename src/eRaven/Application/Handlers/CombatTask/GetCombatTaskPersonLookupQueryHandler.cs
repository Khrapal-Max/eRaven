//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskPersonLookupQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Infrastructure.Repositories.TimesheetRepository;

namespace eRaven.Application.Handlers.CombatTask;

/// <summary>
/// Повертає список осіб, які можуть бути призначені на завдання на дату <see cref="GetCombatTaskPersonLookupQuery.OnDate"/>.
///
/// <para>Критерії "готовий і вільний":</para>
/// <list type="bullet">
/// <item><description>На дату має бути дозвільний табельний код (наприклад стан 30 / "Готовий").</description></item>
/// <item><description>Не має бути активного блокуючого призначення в табелі через <c>TimesheetTaskSpan</c>.</description></item>
/// </list>
///
/// <para>Примітка:</para>
/// <list type="bullet">
/// <item><description>Запит не використовує <c>MissionAssignment</c>, бо це committed-only проєкція факту.</description></item>
/// <item><description><see cref="GetCombatTaskPersonLookupQuery.IsPlanned"/> визначає,
/// чи враховувати планові (Draft) призначення як блокуючі, або працювати по факту (Posted).</description></item>
/// </list>
/// </summary>
public sealed class GetCombatTaskPersonLookupQueryHandler(
    ITimesheetMissionPlanningRepository repo)
    : IQueryHandler<GetCombatTaskPersonLookupQuery, IReadOnlyList<ReadyCombatTaskPersonDto>>
{
    private readonly ITimesheetMissionPlanningRepository _repo = repo;

    /// <inheritdoc />
    public Task<IReadOnlyList<ReadyCombatTaskPersonDto>> HandleAsync(
        GetCombatTaskPersonLookupQuery query,
        CancellationToken ct = default)
        => _repo.GetFreePersonForMissionsAsync(query.OnDate, ct);
}
