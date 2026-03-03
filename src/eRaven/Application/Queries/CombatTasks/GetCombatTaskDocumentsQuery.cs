//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskDocumentsQuery
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.Queries.CombatTasks;

/// <summary>
/// Запит на повернення документів.
/// </summary>
public sealed record GetCombatTaskDocumentsQuery
(
    int Year,
    int Month,
    DocumentStatusDto? Status = null,
    string? Search = null);