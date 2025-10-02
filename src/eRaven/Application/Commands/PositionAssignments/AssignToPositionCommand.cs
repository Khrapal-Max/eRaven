//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AssignToPositionCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.PositionAssignments;

public sealed record AssignToPositionCommand(
    Guid PersonId,
    Guid PositionUnitId,
    DateTime OpenUtc,
    string? Note = null
);