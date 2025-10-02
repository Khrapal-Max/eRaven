//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateStatusKindCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.StatusKinds;

public sealed record CreateStatusKindCommand(
    string Name,
    string Code,
    int Order,
    bool IsActive = true
);
