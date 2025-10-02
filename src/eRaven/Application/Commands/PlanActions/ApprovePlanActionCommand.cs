//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ApprovePlanActionCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.PlanActions;

public sealed record ApprovePlanActionCommand(
    Guid PersonId,
    Guid ActionId,
    string Order
);