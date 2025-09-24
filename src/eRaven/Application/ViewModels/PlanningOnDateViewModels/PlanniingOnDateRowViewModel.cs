//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanniingOnDateRowViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanningOnDateViewModels;

public sealed class PlanniingOnDateRowViewModel
{
    // Рядок людини
    public string? RankName { get; set; }
    public string? FullName { get; set; }
    public string? Callsign { get; set; }

    public string? PlanActionName { get; set; }
    public string? Order { get; set; }
    public DateTime EffectiveAtUtc { get; set; }
    public string? Note { get; set; }
}
