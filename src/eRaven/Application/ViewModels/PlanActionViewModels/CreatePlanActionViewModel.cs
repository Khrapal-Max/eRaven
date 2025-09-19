//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePlanActionViewModel
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.ViewModels.PlanActionViewModels;

public sealed class CreatePlanActionViewModel
{
    public Guid PersonId { get; set; }
    public MoveType MoveType { get; set; } = MoveType.Dispatch;
    public int ToStatusKindId { get; set; }          // автозаповнення з MoveType (2 для Dispatch, 1 для Return; або вибір з довідника)
    public DateTime EffectiveAtUtc { get; set; } = DateTime.UtcNow;

    public Guid? TripId { get; set; }                // для Return — обов'язково; для Dispatch — можна згенерувати
    public string Location { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string CrewName { get; set; } = string.Empty;
    public string? Note { get; set; }
}
