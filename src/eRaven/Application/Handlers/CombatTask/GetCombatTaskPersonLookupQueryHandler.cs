//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskPersonLookupQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class GetCombatTaskPersonLookupQueryHandler(
    IMissionAssignmentRepository repo)
    : IQueryHandler<GetCombatTaskPersonLookupQuery, IReadOnlyList<ReadyCombatTaskPersonDto>>
{
    private readonly IMissionAssignmentRepository _repo = repo;

    public async Task<IReadOnlyList<ReadyCombatTaskPersonDto>> HandleAsync(GetCombatTaskPersonLookupQuery query, CancellationToken ct = default)
        => await _repo.GetFreePersonForMissionsAsync(query.onDate, ct);
}