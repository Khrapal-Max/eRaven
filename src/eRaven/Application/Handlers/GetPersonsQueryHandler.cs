//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers;

public sealed class GetPersonsPageQueryHandler(IPersonRepository repo)
    : IQueryHandler<GetPersonsPageQuery, PagedResult<PersonRowDto>>
{
    private readonly IPersonRepository _repo = repo;

    public async Task<PagedResult<PersonRowDto>> HandleAsync(GetPersonsPageQuery query, CancellationToken ct = default)
        => await _repo.GetPersonsPageAsync(query, ct);
}