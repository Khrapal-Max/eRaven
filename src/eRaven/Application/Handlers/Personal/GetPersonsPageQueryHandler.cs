//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonsPageQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.DTOs.Person;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;

namespace eRaven.Application.Handlers.Personal;

public sealed class GetPersonsPageQueryHandler(IPersonRepository repo)
    : IQueryHandler<GetPersonsPageQuery, PagedResult<PersonListItemDto>>
{
    private readonly IPersonRepository _repo = repo;

    public async Task<PagedResult<PersonListItemDto>> HandleAsync(GetPersonsPageQuery query, CancellationToken ct = default)
        => await _repo.GetPageAsync(query.Page,
            query.PageSize,
            query.Search,
            query.AsOfDate,
            query.Lifecycle,
            query.EnrollmentKind,
            ct);
}
