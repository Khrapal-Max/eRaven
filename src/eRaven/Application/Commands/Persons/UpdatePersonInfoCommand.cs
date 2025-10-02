//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// UpdatePersonInfoCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.Persons;

public sealed record UpdatePersonInfoCommand(
    Guid PersonId,
    string LastName,
    string FirstName,
    string? MiddleName,
    string Rank,
    string BZVP,
    string? Weapon,
    string? Callsign
);