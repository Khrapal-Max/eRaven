//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SetStatusKindActiveCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.StatusKinds;

public sealed record SetStatusKindActiveCommand(
    int StatusKindId,
    bool IsActive
);