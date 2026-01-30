//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskDocumentByIdQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class GetCombatTaskDocumentByIdQueryHandler(
    ICombatTaskDocumentRepository repo)
    : IQueryHandler<GetCombatTaskDocumentByIdQuery, CombatTaskDocumentDetailsDto?>
{
    public async Task<CombatTaskDocumentDetailsDto?> HandleAsync(GetCombatTaskDocumentByIdQuery query, CancellationToken ct = default)
        => await repo.GetByIdAsync(query.DocumentId, ct);
}