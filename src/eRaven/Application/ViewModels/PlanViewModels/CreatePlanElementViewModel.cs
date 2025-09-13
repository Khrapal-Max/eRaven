//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePlanElementViewModel
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.ViewModels.PlanViewModels;

public class CreatePlanElementViewModel
{
    public PlanType Type { get; init; }
    public DateTime EventAtUtc { get; init; }   // 00/15/30/45
    public string? Location { get; init; }
    public string? GroupName { get; init; }
    public string? ToolType { get; init; }
    public string? Note { get; init; }
    public Guid PersonId { get; init; }
}
