//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateMissionCommand
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.Commands.Missions;

/// <summary>
/// Створює місію (Mission).
/// Повертає MissionId.
/// </summary>
public sealed record CreateMissionCommand(
    string PositionArea,
    string? NamePoint,
    string Target,
    MissionModeDto MissionMode,
    string? DroneName,
    DateTime TodayLocal);
