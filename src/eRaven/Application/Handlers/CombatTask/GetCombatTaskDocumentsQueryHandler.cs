//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskDocumentsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class GetCombatTaskDocumentsQueryHandler(ICombatTaskDocumentRepository repo)
    : IQueryHandler<GetCombatTaskDocumentsQuery, IReadOnlyList<CombatTaskDocumentDto>>
{
    private readonly ICombatTaskDocumentRepository _repo = repo;

    public async Task<IReadOnlyList<CombatTaskDocumentDto>> HandleAsync(GetCombatTaskDocumentsQuery query, CancellationToken ct = default)
    {
        var s = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        return await _repo.GetDocumentsAsync(query.Year, query.Month, query.Status, s, ct);
    }
}