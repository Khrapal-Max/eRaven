//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// ActiveMissionPersonDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// DTO активної людини на місії (за даними табеля/TaskSpan).
/// Використовується для "повернути" (rollback) та службових операцій закриття фактів.
/// </summary>
public sealed record ActiveMissionPersonDto(
    Guid CombatTaskDocumentId,
    Guid MissionId,
    Guid PersonId,
    string Rnokpp,
    string FullName,
    string? Callsign,
    string? Rank,
    string? Position,
    string? Weapon,
    DateOnly From);