//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DeleteCombatTaskGroupCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.CombatTask;

public sealed record DeleteCombatTaskGroupCommand(
    Guid DocumentId,
    Guid GroupId);