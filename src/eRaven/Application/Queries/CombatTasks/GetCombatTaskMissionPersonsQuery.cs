//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskMissionPersonsQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.CombatTasks;

/// <summary>
/// Повертає список осіб, які активні по місії на дату.
/// Джерело — табель (derived записи <c>TimesheetEntries</c> з кодом 100), а не документ.
/// </summary>
public sealed record GetCombatTaskMissionPersonsQuery(
    Guid MissionId,
    DateOnly OnDate);
