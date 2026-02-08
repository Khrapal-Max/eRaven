//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskMissionPersonsQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.CombatTask;

public sealed record GetCombatTaskMissionPersonsQuery(
    Guid MissionId,
    DateOnly OnDate
);