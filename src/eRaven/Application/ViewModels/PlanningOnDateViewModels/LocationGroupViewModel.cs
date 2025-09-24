//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// LocationGroupViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanningOnDateViewModels;

public sealed class LocationGroupViewModel
{
    public string Location { get; set; } = string.Empty;
    public List<GroupGroupViewModel> Groups { get; } = new();
}