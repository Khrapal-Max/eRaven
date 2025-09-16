//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// OrderActionViewModel
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.ViewModels.OrderViewModels;

public sealed record OrderActionViewModel(
    Guid Id, Guid OrderId, Guid PlanId, Guid PlanActionId, Guid PersonId,
    PlanActionType ActionType, DateTime EventAtUtc,
    string Location, string GroupName, string CrewName,
    string Rnokpp, string FullName, string RankName, string PositionName,
    string BZVP, string? Weapon, string? Callsign, string StatusKindOnDate);