//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatEntryPersonDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// DTO особи.
///
/// Призначення:
/// - використовується як частина активної місії в документі планування.
/// </summary>
public sealed record CombatEntryPersonDto(
    Guid PersonId,
    string RNOKPP,
    string FullName,
    string Rank,
    string Position,
    string Weapon,
    string Callsign);