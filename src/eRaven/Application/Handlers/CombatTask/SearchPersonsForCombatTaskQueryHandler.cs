//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SearchPersonsForCombatTaskQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class SearchPersonsForCombatTaskQueryHandler(IPersonRepository repo)
    : IQueryHandler<SearchPersonsForCombatTaskQuery, IReadOnlyList<CombatTaskPersonLookupDto>>
{
    private readonly IPersonRepository _repo = repo;
    public async Task<IReadOnlyList<CombatTaskPersonLookupDto>> HandleAsync(
        SearchPersonsForCombatTaskQuery query,
        CancellationToken ct = default)
    {
        var search = string.IsNullOrWhiteSpace(query.Search) ? string.Empty : query.Search.Trim();
        return await _repo.GetPersonsSearchAsync(search, query.Take, ct);
    }
}