//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanDetailsViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanViewModels;

public sealed record PlanDetailsViewModel(PlanViewModel Plan, IReadOnlyList<PlanActionViewModel> Actions);
