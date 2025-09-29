//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanAction
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Models;

/// <summary>
/// Снапшот планової дії щодо людини.
/// </summary>
public sealed class PlanAction
{
    public PlanAction()
    {
    }

    private PlanAction(
        Guid id,
        Guid personId,
        string planActionName,
        DateTime effectiveAtUtc,
        int? toStatusKindId,
        string? order,
        ActionState actionState,
        MoveType moveType,
        string location,
        string groupName,
        string crewName,
        string note,
        string rnokpp,
        string fullName,
        string rankName,
        string positionName,
        string bzvp,
        string weapon,
        string callsign,
        string statusKindOnDate)
    {
        Id = id;
        PersonId = personId;
        PlanActionName = planActionName;
        EffectiveAtUtc = effectiveAtUtc;
        ToStatusKindId = toStatusKindId;
        Order = order;
        ActionState = actionState;
        MoveType = moveType;
        Location = location;
        GroupName = groupName;
        CrewName = crewName;
        Note = note;
        Rnokpp = rnokpp;
        FullName = fullName;
        RankName = rankName;
        PositionName = positionName;
        BZVP = bzvp;
        Weapon = weapon;
        Callsign = callsign;
        StatusKindOnDate = statusKindOnDate;
    }

    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = default!;

    public string PlanActionName { get; set; } = default!;
    public DateTime EffectiveAtUtc { get; set; }
    public int? ToStatusKindId { get; set; }
    public string? Order { get; set; }
    public ActionState ActionState { get; set; }

    public MoveType MoveType { get; set; }
    public string Location { get; set; } = default!;
    public string GroupName { get; set; } = default!;
    public string CrewName { get; set; } = default!;
    public string Note { get; set; } = default!;

    public string Rnokpp { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string RankName { get; set; } = default!;
    public string PositionName { get; set; } = default!;
    public string BZVP { get; set; } = default!;
    public string Weapon { get; set; } = default!;
    public string Callsign { get; set; } = default!;
    public string StatusKindOnDate { get; set; } = default!;

    public static PlanAction Create(
        Guid personId,
        string planActionName,
        DateTime effectiveAtUtc,
        int? toStatusKindId,
        string? order,
        ActionState actionState,
        MoveType moveType,
        string location,
        string groupName,
        string crewName,
        string note,
        string rnokpp,
        string fullName,
        string rankName,
        string positionName,
        string bzvp,
        string weapon,
        string callsign,
        string statusKindOnDate)
    {
        var utc = effectiveAtUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(effectiveAtUtc, DateTimeKind.Utc)
            : effectiveAtUtc.ToUniversalTime();

        return new PlanAction
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            PlanActionName = planActionName,
            EffectiveAtUtc = utc,
            ToStatusKindId = toStatusKindId,
            Order = order,
            ActionState = actionState,
            MoveType = moveType,
            Location = location,
            GroupName = groupName,
            CrewName = crewName,
            Note = note,
            Rnokpp = rnokpp,
            FullName = fullName,
            RankName = rankName,
            PositionName = positionName,
            BZVP = bzvp,
            Weapon = weapon,
            Callsign = callsign,
            StatusKindOnDate = statusKindOnDate
        };
    }
}
