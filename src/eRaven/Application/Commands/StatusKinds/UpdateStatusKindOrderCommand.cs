//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// UpdateStatusKindOrderCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.StatusKinds;

public sealed record UpdateStatusKindOrderCommand(
    int StatusKindId,
    int NewOrder
);
