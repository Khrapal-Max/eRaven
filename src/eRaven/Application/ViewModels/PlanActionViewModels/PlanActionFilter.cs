//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanActionFilter
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.ViewModels.PlanActionViewModels;

public record PlanActionFilter(
    string? QueryText,               // пошук по ПІБ/Позивному/Order/Location
    Guid? PersonId,
    MoveType? MoveType,
    ActionState? ActionState,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page = 1,
    int PageSize = 50
);