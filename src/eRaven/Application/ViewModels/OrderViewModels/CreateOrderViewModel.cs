//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateOrderViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.OrderViewModels;

public record CreateOrderViewModel(
    string Name,
    List<Guid> PlanIds,
    string? Author = null
);
