//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonCardQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers;

public sealed class GetPersonCardQueryHandler(IPersonRepository repo)
    : IQueryHandler<GetPersonCardQuery, PersonDto?>
{
    private readonly IPersonRepository _repo = repo;

    public Task<PersonDto?> HandleAsync(GetPersonCardQuery query, CancellationToken ct = default)
        => _repo.GetPersonByIdAsync(query.PersonId, ct);
}