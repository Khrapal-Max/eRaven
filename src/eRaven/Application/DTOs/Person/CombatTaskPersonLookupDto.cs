//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskPersonLookupDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Person;

/// <summary>
/// Полегшений DTO для picker-а осіб у документі CombatTask.
/// Містить ті ж поля, що пишемо у CombatTaskEntry snapshot.
/// </summary>
public sealed record CombatTaskPersonLookupDto(
    Guid PersonId,
    string RNOKPP,
    string FullName,
    string Rank,
    string Position,
    string Weapon,
    string Callsign
);