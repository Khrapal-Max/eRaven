//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPlanningDayQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.CombatTask;

public sealed record GetPlanningDayQuery(
    DateOnly Date,
    string? Search = null);