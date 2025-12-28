//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Repositories.PositionUnitRepository;

public interface IPositionUnitRepository
{
    Task<IEnumerable<PositionUnit>> GetAllPositionUnits(CancellationToken ct);

    Task AddPositionUnit(PositionUnit positionUnit, CancellationToken ct);

    Task DeActivatedPositionUnit(Guid id, CancellationToken ct);
}