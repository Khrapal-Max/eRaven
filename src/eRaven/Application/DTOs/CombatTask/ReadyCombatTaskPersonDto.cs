//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ReadyPersonDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// Персона, готовий до участі у CombatTask. Видається при пошуку учасників для CombatTask.
/// </summary>
public sealed record ReadyCombatTaskPersonDto(
    Guid PersonId,
    string Rnokpp,
    string FullName,
    string? Rank,
    string? Position,
    string? Weapon,
    string? Callsign);
