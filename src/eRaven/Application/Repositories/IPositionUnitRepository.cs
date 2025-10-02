//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPositionUnitRepository Application layer
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Application.Repositories;

public interface IPositionUnitRepository
{
    PositionUnit? GetById(Guid id);

    Task<IReadOnlyList<PositionUnit>> GetAllActiveAsync(CancellationToken ct = default);
}