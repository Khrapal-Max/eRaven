//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.Missions;

/// <summary>
/// DTO рядка таблиці місій (/missions).
/// Призначення: швидкий вивід реєстру (активні/всі, пошук).
/// </summary>
public sealed record MissionDto(
    Guid MissionId,
    string PositionArea,
    string? NamePoint,
    string Target,
    MissionMode MissionMode,
    string? DroneName,
    string DisplayMisssion,
    DateOnly CreatedAt,
    DateOnly? ClosedAt,
    bool IsOpen);
