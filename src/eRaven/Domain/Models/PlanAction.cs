//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanAction
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Models;

public class PlanAction
{
    public Guid Id { get; set; }

    public Guid PersonId { get; set; }
    public Person Person { get; set; } = default!;

    public string PlanActionName { get; set; } = default!;
    public DateTime EffectiveAtUtc { get; set; }
    public int? ToStatusKindId { get; set; }
    public string? Order { get; set; }

    // Стан дії до/після наказу
    public ActionState ActionState { get; set; } = ActionState.PlanAction; // Draft | Approved | Superseded

    // Снапшот планової дії
    public MoveType MoveType { get; set; } // Dispatch | Return
    public string Location { get; set; } = default!;
    public string GroupName { get; set; } = default!;
    public string CrewName { get; set; } = default!;
    public string Note { get; set; } = default!;


    // Снапшот залишаємо як є (можна перевести в Owned type)
    public string Rnokpp { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string RankName { get; set; } = default!;
    public string PositionName { get; set; } = default!;
    public string BZVP { get; set; } = default!;
    public string Weapon { get; set; } = default!;
    public string Callsign { get; set; } = default!;
    public string StatusKindOnDate { get; set; } = default!;
}
