//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionModeDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Enums;

/// <summary>
/// DTO режиму готовності для виконання завдань.
/// </summary>
public enum MissionModeDto : byte
{
    Day = 0,
    Night = 1,
    FullTime = 2
}
