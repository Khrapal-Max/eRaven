//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// UpdatePersonalInfoCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.PersonInfo;

public sealed record UpdatePersonalInfoCommand(
    Guid PersonId,
    string Rnokpp,
    string LastName,
    string FirstName,
    string? MiddleName,
    string Author,
    DateTime NowUtc);