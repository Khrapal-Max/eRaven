//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetAllPositionsQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.Positions;

public sealed record GetAllPositionsQuery(bool OnlyActive = true);