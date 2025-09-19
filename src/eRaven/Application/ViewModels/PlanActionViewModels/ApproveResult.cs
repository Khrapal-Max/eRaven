//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ApproveResult
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanActionViewModels;

public record ApproveResult(
    Guid ActionId,
    bool Applied,
    IReadOnlyList<string> Errors
);