//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonRepository Application layer
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using System.Linq.Expressions;

namespace eRaven.Application.Repositories;

public interface IPersonRepository
{
    Task<PersonAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<PersonAggregate?> GetByRnokppAsync(string rnokpp, CancellationToken ct = default);

    Task<IReadOnlyList<PersonAggregate>> SearchAsync(
    Expression<Func<PersonAggregate, bool>>? predicate,
    CancellationToken ct = default);

    Task AddAsync(PersonAggregate person, CancellationToken ct = default);

    Task UpdateAsync(PersonAggregate person, CancellationToken ct = default);

    bool IsPositionOccupied(Guid positionUnitId);
}