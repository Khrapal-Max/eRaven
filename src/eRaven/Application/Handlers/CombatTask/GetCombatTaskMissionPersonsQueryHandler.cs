//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskMissionPersonsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Infrastructure.Repositories.TimesheetRepository;

namespace eRaven.Application.Handlers.CombatTask;

/// <summary>
/// Повертає список осіб, які перебувають на місії на конкретну дату.
///
/// <para>Джерело істини:</para>
/// <list type="bullet">
/// <item><description><c>TimesheetTaskSpan</c> (Draft/Posted) у табелі.</description></item>
/// </list>
///
/// <para>Примітка:</para>
/// <list type="bullet">
/// <item><description>Цей запит не використовує <c>MissionAssignment</c>, оскільки це committed-only проєкція.</description></item>
/// <item><description><paramref name="GetCombatTaskMissionPersonsQuery.IsPlanned"/> визначає,
/// чи повертати план (Draft) або факт (Posted).</description></item>
/// </list>
/// </summary>
public sealed class GetCombatTaskMissionPersonsQueryHandler(
    ITimesheetMissionPlanningRepository repo)
    : IQueryHandler<GetCombatTaskMissionPersonsQuery, IReadOnlyList<ActiveMissionPersonDto>>
{
    private readonly ITimesheetMissionPlanningRepository _repo = repo;

    /// <inheritdoc />
    public Task<IReadOnlyList<ActiveMissionPersonDto>> HandleAsync(
        GetCombatTaskMissionPersonsQuery query,
        CancellationToken ct = default)
        => _repo.GetActiveByMissionAsync(query.MissionId, query.OnDate, query.IsPlanned, ct);
}
