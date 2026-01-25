//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPlanningDayQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class GetPlanningDayQueryHandler(ICombatTaskReadRepository repo)
    : IQueryHandler<GetPlanningDayQuery, IReadOnlyList<PlanningDayGroupDto>>
{
    private readonly ICombatTaskReadRepository _repo = repo;

    public async Task<IReadOnlyList<PlanningDayGroupDto>> HandleAsync(GetPlanningDayQuery query, CancellationToken ct = default)
    {
        var s = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        return await _repo.GetPlanningDayAsync(query.Date, s, ct);
    }
}