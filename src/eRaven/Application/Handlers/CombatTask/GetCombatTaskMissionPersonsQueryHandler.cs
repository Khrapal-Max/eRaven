//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskMissionPersonsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class GetCombatTaskMissionPersonsQueryHandler(
    IMissionAssignmentRepository repo)
    : IQueryHandler<GetCombatTaskMissionPersonsQuery, IReadOnlyList<ActiveMissionPersonDto>>
{
    private readonly IMissionAssignmentRepository _repo = repo;

    public async Task<IReadOnlyList<ActiveMissionPersonDto>> HandleAsync(
        GetCombatTaskMissionPersonsQuery query,
        CancellationToken ct = default)
    => await _repo.GetActiveByMissionAsync(query.MissionId, query.OnDate, ct);
}
