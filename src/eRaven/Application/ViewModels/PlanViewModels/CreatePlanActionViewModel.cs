//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePlanActionViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanViewModels;

public record CreatePlanActionModel(
    Guid PersonId,
    DateTime EffectiveAtUtc,
    int ToStatusKindId,
    string Note // опціонально
);
