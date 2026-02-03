//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EndCombatTaskGroupModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// UI-модель закриття групи участей.
/// </summary>
public sealed record EndCombatTaskGroupModel(
    Guid GroupId,
    DateOnly To,
    string EndSourceDocNo);
