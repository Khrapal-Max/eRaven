//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ReplaceCombatTaskGroupPersonsCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.CombatTask;

public sealed record ReplaceCombatTaskGroupPersonsCommand(
    Guid DocumentId,
    Guid GroupId,
    IReadOnlyCollection<Guid> PersonIds,
    string Author,
    DateTime NowUtc);
