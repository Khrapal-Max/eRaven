//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonalHistoriesQueryHandlers
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers.Personal;

public class GetPersonalHistoriesQueryHandlers(IPersonRepository repo)
    : IQueryHandler<GetPersonDetailsQuery, IReadOnlyCollection<PersonEventDto?>>
{
    private readonly IPersonRepository _repo = repo;

    public async Task<IReadOnlyCollection<PersonEventDto?>> HandleAsync(GetPersonDetailsQuery query, CancellationToken ct = default)
        => await _repo.GetHistoryAsync(query.PersonId, ct);
}