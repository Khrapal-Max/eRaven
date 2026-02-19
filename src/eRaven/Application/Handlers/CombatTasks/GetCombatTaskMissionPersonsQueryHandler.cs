//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskMissionPersonsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.CombatTasks;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTasks;

namespace eRaven.Application.Handlers.CombatTasks;

/// <summary>
/// Query handler: повертає людей, які активні по місії на дату (за табелем).
/// </summary>
public sealed class GetCombatTaskMissionPersonsQueryHandler(
    ITimesheetMissionPlanningRepository repo)
    : IQueryHandler<GetCombatTaskMissionPersonsQuery, IReadOnlyList<ActiveMissionPersonDto>>
{
    private readonly ITimesheetMissionPlanningRepository _repo = repo;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ActiveMissionPersonDto>> HandleAsync(
        GetCombatTaskMissionPersonsQuery query,
        CancellationToken ct = default)
        => await _repo.GetActiveMissionClosablePersonsAsync(query.MissionId, query.OnDate, ct);
}
