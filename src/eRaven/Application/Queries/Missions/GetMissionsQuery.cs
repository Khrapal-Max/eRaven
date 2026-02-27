//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetMissionsQuery
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.Queries.Missions;

/// <summary>
/// Повертає список місій для UI (таблиця).
/// </summary>
public sealed record GetMissionsQuery(
    bool OnlyOpen,
    string? Search,
    MissionModeDto? Mode);
