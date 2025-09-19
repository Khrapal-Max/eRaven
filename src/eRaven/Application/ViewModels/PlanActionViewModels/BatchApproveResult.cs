//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// BatchApproveResult
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanActionViewModels;

public record BatchApproveResult(
    int Requested,
    int Applied,
    IReadOnlyList<ApproveResult> PerAction
);