//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StartCombatTaskGroupModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// UI-модель створення групи участей.
/// </summary>
public sealed record StartCombatTaskGroupModel(
    string SourceDocNo,
    Guid MissionId,
    string MissionDisplaySnapshot,
    DateOnly From,
    IReadOnlyList<CombatTaskPersonLookupDto> Persons);