//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ApproveOptions
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanActionViewModels;

public record ApproveOptions(
    string OrderName,                 // номер/назва наказу (PlanAction.Order)
    string? Author = null
);