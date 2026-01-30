//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatGroupModel
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

public sealed record CreateCombatGroupModel(
    string SourceDocNo,
    ActionKind Action,
    Guid MissionId,
    string MissionDisplaySnapshot,
    DateOnly ActionDate,
    IReadOnlyList<Guid> PersonIds);
