//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateMissionCommand
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Commands.Mission;

/// <summary>
/// Створює місію (Mission).
/// Повертає MissionId.
/// </summary>
public sealed record CreateMissionCommand(
    string PositionArea,
    string? NamePoint,
    string Target,
    MissionMode MissionMode,
    string? DroneName,
    DateTime TodayLocal
);