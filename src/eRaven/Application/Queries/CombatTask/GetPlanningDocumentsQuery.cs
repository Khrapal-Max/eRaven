//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPlanningDocumentsQuery
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Queries.CombatTask;

public sealed record GetPlanningDocumentsQuery(
    int Year,
    int Month,
    CombatTaskPlanDocumentStatus? Status = null,
    string? Search = null);
