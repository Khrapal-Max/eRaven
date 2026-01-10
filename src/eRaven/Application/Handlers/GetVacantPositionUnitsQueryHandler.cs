//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetVacantPositionUnitsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Infrastructure.Repositories.PositionUnitRepository;

namespace eRaven.Application.Handlers;

public sealed class GetVacantPositionUnitsQueryHandler(IPositionUnitRepository repo)
    : IQueryHandler<GetVacantPositionUnitsQuery, IReadOnlyList<PositionUnitOptionDto>>
{
    private readonly IPositionUnitRepository _repo = repo;

    public async Task<IReadOnlyList<PositionUnitOptionDto>> HandleAsync(GetVacantPositionUnitsQuery query, CancellationToken ct = default)
        => await _repo.GetVacantOptionsAsync(query.Search, query.Take, ct);
}