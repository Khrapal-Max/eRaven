//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetMissionsQuery
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Queries.Mission;

/// <summary>
/// Повертає список місій для UI (таблиця).
/// </summary>
public sealed record GetMissionsQuery(
    bool OnlyOpen,
    string? Search,
    MissionMode? Mode
);