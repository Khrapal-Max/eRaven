//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// OrderDetailsViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.OrderViewModels;

public sealed record OrderDetailsViewModel(
    OrderViewModel Order,
    IReadOnlyList<Guid> PlanIds,
    IReadOnlyList<OrderActionViewModel> Actions
);