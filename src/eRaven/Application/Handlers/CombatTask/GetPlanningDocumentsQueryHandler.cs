//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPlanningDocumentsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class GetPlanningDocumentsQueryHandler(ICombatTaskReadRepository repo)
    : IQueryHandler<GetPlanningDocumentsQuery, IReadOnlyList<PlanningDocumentRowDto>>
{
    private readonly ICombatTaskReadRepository _repo = repo;

    public async Task<IReadOnlyList<PlanningDocumentRowDto>> HandleAsync(GetPlanningDocumentsQuery query, CancellationToken ct = default)
    {
        var s = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        return await _repo.GetPlanningDocumentsAsync(query.Year, query.Month, query.Status, s, ct);
    }
}