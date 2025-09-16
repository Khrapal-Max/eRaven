//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Order
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Models;

public class OrderAction
{
    public Guid Id { get; set; }

    // Наказ
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;

    // Джерело (опційно, але корисно для трасування)
    public Guid PlanId { get; set; }
    public Guid PlanActionId { get; set; }

    // Особа
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = default!;

    // Підтверджена дія (копія з PlanAction)
    public PlanActionType ActionType { get; set; }
    public DateTime EventAtUtc { get; set; }            // UTC
    public string Location { get; set; } = default!;
    public string GroupName { get; set; } = default!;
    public string CrewName { get; set; } = default!;

    // Знімок особи на момент дії (копія в наказі)
    public string Rnokpp { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string RankName { get; set; } = default!;
    public string PositionName { get; set; } = default!;
    public string BZVP { get; set; } = default!;
    public string Weapon { get; set; } = default!;
    public string Callsign { get; set; } = default!;
    public string StatusKindOnDate { get; set; } = default!;
}
