//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonDetailsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers;

public sealed class GetPersonDetailsQueryHandler(IPersonRepository repo)
    : IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>
{
    private readonly IPersonRepository _repo = repo;

    public async Task<PersonDetailsDto?> HandleAsync(GetPersonDetailsQuery query, CancellationToken ct = default)
        => await _repo.GetByIdAsync(query.PersonId, ct);
}