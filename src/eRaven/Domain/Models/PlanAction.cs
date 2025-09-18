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
    public Guid PlanId { get; set; }
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = default!;

    public DateTime EffectiveAtUtc { get; set; }

    public int ToStatusKindId { get; set; }

    public Guid? TripId { get; set; }   // один Trip = Dispatch→Return

    // Стан дії до/після наказу
    public PlanActionState State { get; set; } = PlanActionState.Draft; // Draft | Approved | Superseded

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
