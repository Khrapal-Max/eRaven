//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PostCombatTaskPlanDocumentCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.CombatTask;

public sealed record PostCombatTaskPlanDocumentCommand(
    Guid DocumentId,
    string Author,
    DateTime NowUtc
);
