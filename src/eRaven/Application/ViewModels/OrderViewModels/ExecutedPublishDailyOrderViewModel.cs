//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// ExecutedPublishDailyOrderViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.OrderViewModels;

public sealed record ExecutedPublishDailyOrderViewModel(
    OrderViewModel Order,
    IReadOnlyList<Guid> ClosedPlanIds,
    IReadOnlyList<OrderActionViewModel> ConfirmedActions
);
