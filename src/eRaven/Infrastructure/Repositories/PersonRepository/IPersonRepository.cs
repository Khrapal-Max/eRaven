//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Domain.Aggregates;

namespace eRaven.Infrastructure.Repositories.PersonRepository;

public interface IPersonRepository
{
    Task<PersonAggregate?> LoadAsync(Guid id, CancellationToken ct = default);

    Task<PagedResult<PersonTableDto>> GetPersonsPageAsync(GetPersonsPageQuery query, CancellationToken ct = default);

    Task<PersonDto?> GetPersonByIdAsync(Guid id, CancellationToken ct = default);

    Task SaveAsync(PersonAggregate agg, long expectedVersion, CancellationToken ct = default);
}
