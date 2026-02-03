//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SearchFreePersonsForCombatTaskQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.CombatTask;

/// <summary>
/// Повертає осіб, які НЕ мають активної участі в місії на дату.
/// Використовується для picker-а.
/// </summary>
public sealed record SearchFreePersonsForCombatTaskQuery(
    DateOnly Date,
    string? Search,
    int Take = 50
);