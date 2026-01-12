//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollCommand
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Commands.PersonMove;

public sealed record EnrollCommand(
    Guid PersonId,
    EnrollmentKind Kind,
    string? Reference,
    string Reason,
    DateOnly EnrollDate,
    string Position,
    string Author,
    DateTime NowUtc);
