//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateReservedCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.PersonMove;

public sealed record CreateReservedCommand(
    Guid PersonId,
    string Rnokpp,
    string LastName,
    string FirstName,
    string? MiddleName,
    string? Rank,
    int? PositionSort,
    string? Position,
    string Author,
    DateTime NowUtc);