//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskMissionBlockDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.CombatTasks;

/// <summary>
/// Один блок по місії (відповідає CombatTask).
/// </summary>
public sealed record CombatTaskMissionBlockDto(
    Guid CombatTaskId,
    Guid MissionId,
    string MissionName,
    string SourceDocument,
    IReadOnlyList<CombatTaskDetailsDto> CombatTaskDetails);
