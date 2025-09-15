//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanBatchViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanViewModels;

public class PlanBatchViewModel
{
    public string PlanNumber { get; set; } = default!;

    public IReadOnlyList<PlanActionViewModel> Actions = [];
}
