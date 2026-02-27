//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollCommand
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.Commands.PersonMove;

public sealed record EnrollCommand(
    Guid PersonId,
    EnrollmentKindDto Kind,
    string? Reference,
    string Reason,
    DateOnly EnrollDate,
    string Rank,
    int PositionSort,
    string Position,
    string Author,
    DateTime NowUtc);
