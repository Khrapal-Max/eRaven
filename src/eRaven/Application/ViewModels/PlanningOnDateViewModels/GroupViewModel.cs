//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// GroupGroupViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanningOnDateViewModels;

public sealed class GroupViewModel
{
    public string GroupName { get; set; } = string.Empty;
    public List<CrewGroupViewModel> Crews { get; } = new();
}
