//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionGroupModel
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.Mission;

/// <summary>
/// Форма призначена для показу місій.
/// </summary>
public sealed record MissionGroupModel(MissionMode Mode, IReadOnlyList<MissionDto> Items)
{
    public int Count => Items.Count;
}
