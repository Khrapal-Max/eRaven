//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePlanActionDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.ViewModels.PlanActionViewModels;

public record CreatePlanActionDto
(
    Guid PersonId,
    MoveType MoveType,                // Dispatch | Return
    int ToStatusKindId,               // 2: "В БР" для Dispatch, 1: "В районі" для Return, тощо
    DateTime EffectiveAtUtc,
    Guid? TripId,                     // якщо не заданий — можемо генерувати для Dispatch
    string Location,
    string GroupName,
    string CrewName,
    string? Note
);
