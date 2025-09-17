//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonEligibilityViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PersonViewModels;

/// <summary>
/// Кандидат для додавання в план із позначкою «можна/не можна» і причиною.
/// </summary>
public sealed record PersonEligibilityViewModel(
    Guid Id,
    string FullName,
    string Rnokpp,
    string Rank,
    string PositionFullName,
    string BZVP,
    string? Weapon,
    string? Callsign,
    bool IsEligible,          // true — можна додати для обраної дії
    string? IneligibilityReason // якщо IsEligible=false — коротке пояснення
);
