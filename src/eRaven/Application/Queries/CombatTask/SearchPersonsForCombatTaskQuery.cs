//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SearchPersonsForCombatTaskQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.CombatTask;

/// <summary>
/// Пошук осіб для вибору у CombatTask drawer.
/// Повертає невеликий список (top N) з потрібними полями.
/// </summary>
public sealed record SearchPersonsForCombatTaskQuery(
    string? Search,
    int Take = 50
);