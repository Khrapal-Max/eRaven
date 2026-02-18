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
