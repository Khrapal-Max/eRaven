//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePersonCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.Persons;

public sealed record CreatePersonCommand(
    string Rnokpp,
    string LastName,
    string FirstName,
    string? MiddleName,
    string Rank,
    string BZVP,
    string? Weapon,
    string? Callsign
);