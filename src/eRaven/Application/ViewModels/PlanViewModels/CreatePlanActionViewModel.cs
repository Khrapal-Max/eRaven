//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePlanActionViewModel
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.ViewModels.PlanViewModels;

public record class CreatePlanActionViewModel
{
    public Guid PlanId { get; set; }
    public Guid PersonId { get; set; }

    public PlanActionType ActionType { get; set; } = PlanActionType.Dispatch;
    public DateTime EventAtUtc { get; set; } = DateTime.UtcNow;

    public string Location { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string CrewName { get; set; } = string.Empty;
}