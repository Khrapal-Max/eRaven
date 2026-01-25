//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPlanningMonthQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.CombatTask;

public sealed record GetPlanningMonthQuery(
    int Year,
    int Month,
    string? Search = null);
