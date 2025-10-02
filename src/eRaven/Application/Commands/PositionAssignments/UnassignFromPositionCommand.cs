//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// UnassignFromPositionCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.PositionAssignments;

public sealed record UnassignFromPositionCommand(
    Guid PersonId,
    DateTime CloseUtc,
    string? Note = null
);