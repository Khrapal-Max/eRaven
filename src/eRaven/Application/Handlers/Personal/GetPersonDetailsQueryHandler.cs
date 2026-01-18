//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonDetailsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers.Personal;

public sealed class GetPersonDetailsQueryHandler(IPersonRepository repo)
    : IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>
{
    private readonly IPersonRepository _repo = repo;

    public async Task<PersonDetailsDto?> HandleAsync(GetPersonDetailsQuery query, CancellationToken ct = default)
        => await _repo.GetByIdAsync(query.PersonId, ct);
}