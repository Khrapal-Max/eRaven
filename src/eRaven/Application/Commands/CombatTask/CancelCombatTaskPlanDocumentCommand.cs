//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CancelCombatTaskPlanDocumentCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.CombatTask;

public sealed record CancelCombatTaskPlanDocumentCommand(
    Guid DocumentId,
    string Reason,
    string Author,
    DateTime NowUtc
);