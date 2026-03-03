//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDetailsDto
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.DTOs.CombatTasks;

/// <summary>
/// Рядок документа (CombatTaskDetails).
/// </summary>
public sealed record CombatTaskDetailsDto(
    Guid CombatTaskDetailsId,
    CombatTaskDetailsKindDto Kind,
    DateOnly EffectiveAt,
    Guid PersonId,
    string Rnokpp,
    string FullName,
    string? Rank,
    string? Position,
    string? Weapon,
    string? Callsign);
