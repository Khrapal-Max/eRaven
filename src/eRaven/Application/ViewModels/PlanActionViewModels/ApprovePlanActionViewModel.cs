//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// ApprovePlanActionRequest
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.ViewModels.PlanActionViewModels;

public sealed class ApprovePlanActionViewModel
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public DateTime EffectiveAtUtc { get; set; }
    public string Order { get; set; } = string.Empty;
    public MoveType MoveType { get; set; }
}