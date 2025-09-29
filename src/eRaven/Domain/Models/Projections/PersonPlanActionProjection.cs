//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Person (Aggregate Root)
//-----------------------------------------------------------------------------


//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Person (Aggregate Root)
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Models.Projections;

/// <summary>
/// Проекція планової дії.
/// </summary>
public sealed record PersonPlanActionProjection(
    Guid PlanActionId,
    string PlanActionName,
    DateTime EffectiveAtUtc,
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
    string StatusKindOnDate);
