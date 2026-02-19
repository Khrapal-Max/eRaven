//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// ActiveMissionPersonDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.CombatTasks;

/// <summary>
/// Використовується для відображення осіб, 
/// які були активними у місії на певну дату 
/// (для вибору осіб при завершенні участі в місії).
/// 
/// В CloseCombatTaskDrawer
/// </summary>
public sealed record ActiveMissionPersonDto(
    Guid PersonId,
    string Rnokpp,
    string FullName,
    string? Callsign,
    string? Rank,
    string? Position,
    string? Weapon,
    DateOnly From
);