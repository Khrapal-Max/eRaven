//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// UpdatePlanActionDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanActionViewModels;

public record UpdatePlanActionDto(
    Guid Id,
    DateTime EffectiveAtUtc,
    int ToStatusKindId,
    string Location,
    string GroupName,
    string CrewName,
    string? Note
);
