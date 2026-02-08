//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskDetailsByDocumentIdQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class GetCombatTaskDetailsByDocumentIdQueryHandler(ICombatTaskRepository repo)
    : IQueryHandler<GetCombatTaskDetailsByDocumentIdQuery, CombatTaskEditorDto?>
{
    private readonly ICombatTaskRepository _repo = repo;

    public async Task<CombatTaskEditorDto?> HandleAsync(GetCombatTaskDetailsByDocumentIdQuery query, CancellationToken ct = default)
    {
        return await _repo.GetDocumentEditorAsync(query.DocumentId, ct);
    }
}