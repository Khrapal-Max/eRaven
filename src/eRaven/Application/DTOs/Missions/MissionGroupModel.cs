//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionGroupModel
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.DTOs.Missions;

/// <summary>
/// Форма призначена для показу місій.
/// </summary>
public sealed record MissionGroupModel(MissionModeDto Mode, IReadOnlyList<MissionDto> Items)
{
    public int Count => Items.Count;
}
