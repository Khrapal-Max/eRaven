//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActionRecordedEvent
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Events;

/// <summary>
/// Подія створення планової дії з повним snapshot (частина історії агрегату)
/// </summary>
public class PlanActionRecordedEvent
{
    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }

    // Plan Data
    public string PlanActionName { get; private set; }
    public DateTime EffectiveAtUtc { get; private set; }
    public ActionState ActionState { get; private set; }
    public MoveType MoveType { get; private set; }

    public string Location { get; private set; }
    public string? GroupName { get; private set; }
    public string? CrewName { get; private set; }
    public string? Note { get; private set; }
    public string? Order { get; private set; }

    // Snapshot (для історичної точності)
    public string Rnokpp { get; private set; }
    public string FullName { get; private set; }
    public string RankName { get; private set; }
    public string? Callsign { get; private set; }
    public string BZVP { get; private set; }
    public string? Weapon { get; private set; }
    public string PositionName { get; private set; }
    public string StatusKindOnDate { get; private set; }

    public DateTime RecordedAt { get; private set; }

    private PlanActionRecordedEvent()
    {
        PlanActionName = string.Empty;
        Location = string.Empty;
        Rnokpp = string.Empty;
        FullName = string.Empty;
        RankName = string.Empty;
        BZVP = string.Empty;
        PositionName = string.Empty;
        StatusKindOnDate = string.Empty;
    }

    internal PlanActionRecordedEvent(
        Guid personId,
        string planActionName,
        DateTime effectiveAtUtc,
        MoveType moveType,
        string location,
        string? groupName,
        string? crewName,
        string? note,
        // Snapshot
        string rnokpp,
        string fullName,
        string rankName,
        string? callsign,
        string bzvp,
        string? weapon,
        string positionName,
        string statusKindOnDate)
    {
        Id = Guid.NewGuid();
        PersonId = personId;
        PlanActionName = planActionName;
        EffectiveAtUtc = effectiveAtUtc;
        ActionState = ActionState.PlanAction;
        MoveType = moveType;
        Location = location;
        GroupName = groupName;
        CrewName = crewName;
        Note = note;

        Rnokpp = rnokpp;
        FullName = fullName;
        RankName = rankName;
        Callsign = callsign;
        BZVP = bzvp;
        Weapon = weapon;
        PositionName = positionName;
        StatusKindOnDate = statusKindOnDate;

        RecordedAt = DateTime.UtcNow;
    }

    internal void Approve(string order)
    {
        if (ActionState == ActionState.ApprovedOrder)
            throw new InvalidOperationException("Дія вже затверджена");

        if (string.IsNullOrWhiteSpace(order))
            throw new ArgumentException("Номер наказу обов'язковий");

        Order = order.Trim();
        ActionState = ActionState.ApprovedOrder;
    }
}