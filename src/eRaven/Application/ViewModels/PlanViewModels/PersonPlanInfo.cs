//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanContracts (DTO для точкових операцій і даних модала)
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.ViewModels.PlanViewModels;

public sealed record PersonPlanInfo(
    Guid PersonId,
    string FullName,
    string Rnokpp,
    string? Rank,
    string? Position,
    string? Weapon,
    string? Callsign,
    int? StatusKindId,
    string? StatusKindCode,
    string? StatusKindName,
    PlanType? LastPlannedAction,          // остання дія цієї особи в межах плану (якщо є)
    string? LastDispatchLocation,         // контекст останнього Dispatch (для автозаповнення Return)
    string? LastDispatchGroup,
    string? LastDispatchTool
);
