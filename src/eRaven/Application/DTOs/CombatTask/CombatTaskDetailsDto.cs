//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDetailsDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// Рядок документа (CombatTaskDetails).
/// </summary>
public sealed record CombatTaskDetailsDto(
    Guid CombatTaskDetailsId,
    CombatTaskDetailsKind Kind,
    DateOnly EffectiveAt,
    Guid PersonId,
    string Rnokpp,
    string FullName,
    string? Callsign);
