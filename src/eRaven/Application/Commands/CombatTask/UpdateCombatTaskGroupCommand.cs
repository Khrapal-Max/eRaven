//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// UpdateCombatTaskGroupCommand
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Commands.CombatTask;

public sealed record UpdateCombatTaskGroupCommand(
    Guid DocumentId,
    Guid GroupId,
    string SourceDocNo,
    ActionKind Action,
    Guid MissionId,
    string MissionDisplaySnapshot,
    DateOnly ActionDate,
    string Author,
    DateTime NowUtc
);