//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionGroupDto
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Mission;
using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// Група місій для UI (optgroup).
/// </summary>
public sealed record MissionGroupDto(
    MissionMode Mode,
    IReadOnlyList<MissionDto> Items)
{
    public int Count => Items.Count;
}