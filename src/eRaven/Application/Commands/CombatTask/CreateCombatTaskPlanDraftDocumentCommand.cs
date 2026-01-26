//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskPlanDraftDocumentCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.CombatTask;

public sealed record CreateCombatTaskPlanDraftDocumentCommand(
    Guid DocumentId,
    DateOnly RecordedAt,
    DateOnly PlanningDate,
    string PlanningDocTitle,
    string Author,
    DateTime NowUtc
);