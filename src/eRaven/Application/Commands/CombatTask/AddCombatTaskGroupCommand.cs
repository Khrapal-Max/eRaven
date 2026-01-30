//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AddCombatTaskGroupCommand
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Commands.CombatTask;

public sealed record AddCombatTaskGroupCommand(
    Guid DocumentId,
    string SourceDocNo,
    ActionKind Action,
    Guid MissionId,
    string MissionDisplaySnapshot,
    DateOnly ActionDate,
    IReadOnlyCollection<Guid> PersonIds,
    string Author,
    DateTime NowUtc);