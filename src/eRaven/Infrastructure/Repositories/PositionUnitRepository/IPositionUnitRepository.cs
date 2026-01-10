//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Repositories.PositionUnitRepository;

public interface IPositionUnitRepository
{
    Task<IEnumerable<PositionUnit>> GetAllPositionUnitsAsync(CancellationToken ct);

    Task<IReadOnlyList<PositionUnitOptionDto>> GetVacantOptionsAsync(
       string? search,
       int take,
       CancellationToken ct = default);


    Task AddPositionUnitAsync(PositionUnit positionUnit, CancellationToken ct);

    Task DeActivatedPositionUnitAsync(Guid id, CancellationToken ct);

    Task<bool> CodeExistsAsync(string code, CancellationToken ct);

    Task<bool> ActiveNumberExistsAsync(int number, CancellationToken ct);
}