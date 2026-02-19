//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskPersonLookupQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.CombatTasks;

/// <summary>
/// Повертає список осіб, які можуть бути призначені на завдання на дату <paramref name="OnDate"/>.
/// Джерело правди — факти табеля (поточний код + відсутність активного TaskSpan).
/// </summary>
public record GetCombatTaskPersonLookupQuery(DateOnly OnDate);
