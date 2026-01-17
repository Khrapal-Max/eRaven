//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonalHistoriesQueryHandlers
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Presenter;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers.Personal;

public sealed class GetPersonHistoryQueryHandler(
    IPersonRepository repo,
    IPersonEventPresenter presenter)
    : IQueryHandler<GetPersonHistoryQuery, IReadOnlyList<PersonEventListItemDto>>
{
    public async Task<IReadOnlyList<PersonEventListItemDto>> HandleAsync(GetPersonHistoryQuery query, CancellationToken ct = default)
    {
        var raw = await repo.GetHistoryAsync(query.PersonId, ct); 
        return [.. raw.Select(presenter.ToListItem)];
    }
}