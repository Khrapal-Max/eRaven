//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActionViewModel
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.ViewModels.PlanViewModels;

public class PlanActionViewModel
{
    public Guid PlanId { get; set; }
    public Guid PersonId { get; set; }

    public PlanActionType ActionType { get; set; }      // Dispatch/Return
    public DateTime EventAtUtc { get; set; }            // вже в UTC
    public string Location { get; set; } = default!;
    public string GroupName { get; set; } = default!;
    public string CrewName { get; set; } = default!;
}
