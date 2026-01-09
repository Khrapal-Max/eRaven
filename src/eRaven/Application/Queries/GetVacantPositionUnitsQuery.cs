//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetVacantPositionUnitsQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries;

public record GetVacantPositionUnitsQuery(
    string? Search = null,
    int Take = 50
);