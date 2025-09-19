//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ApproveOptionsViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanActionViewModels;

public sealed class ApproveOptionsViewModel
{
    public string OrderName { get; set; } = string.Empty;
    public string? Author { get; set; }
    public List<Guid> SelectedActionIds { get; set; } = new();
}
