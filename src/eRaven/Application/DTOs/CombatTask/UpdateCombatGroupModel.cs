//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// UpdateCombatGroupModel
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

public sealed record UpdateCombatGroupModel(
    Guid GroupId,
    string SourceDocNo,
    ActionKind Action,
    Guid MissionId,
    string MissionDisplaySnapshot,
    DateOnly ActionDate);
