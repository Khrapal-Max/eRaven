//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Events;

public sealed record PlanActionAddedEvent(
    Guid PersonId,
    Guid PlanActionId,
    string PlanActionName,
    DateTime OccurredAtUtc,
    int? ToStatusKindId,
    string? Order,
    ActionState ActionState,
    MoveType MoveType,
    string Location,
    string GroupName,
    string CrewName,
    string Note,
    string Rnokpp,
    string FullName,
    string RankName,
    string PositionName,
    string BZVP,
    string Weapon,
    string Callsign,
    string StatusKindOnDate) : IPersonEvent;
