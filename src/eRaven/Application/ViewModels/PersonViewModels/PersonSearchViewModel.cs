//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonService
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PersonViewModels;

public record PersonSearchViewModel(
        Guid Id,
        string FullName,
        string Rnokpp,
        string Rank,
        string PositionFullName,
        string BZVP,
        string? Weapon,
        string? Callsign
    );