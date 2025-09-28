//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// CrewGroupViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanningOnDateViewModels;

public sealed class CrewGroupViewModel
{
    public string CrewName { get; set; } = string.Empty;
    public List<PlanniingOnDateRowViewModel> Rows { get; } = new();
}