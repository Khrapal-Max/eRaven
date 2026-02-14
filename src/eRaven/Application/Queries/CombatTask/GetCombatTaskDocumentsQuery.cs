//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskDocumentsQuery
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Queries.CombatTask;

public sealed record GetCombatTaskDocumentsQuery
(
    int Year,
    int Month,
    DocumentStatus? Status = null,
    string? Search = null);
