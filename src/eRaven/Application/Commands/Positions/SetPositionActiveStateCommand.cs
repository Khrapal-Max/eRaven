//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SetPositionActiveStateCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.Positions;

public sealed record SetPositionActiveStateCommand(
    Guid PositionId,
    bool IsActive
);