//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanningDocumentRowDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

public sealed record PlanningDocumentRowDto(
    Guid DocumentId,
    DateOnly DocumentDate,
    string Title,
    CombatTaskPlanDocumentStatus Status,
    int PersonsCount,
    DateOnly? MinStart,
    DateOnly? MaxEnd,
    DateTime CreatedAtUtc
);