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

    // План
    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = default!;

    // 🔴 Явний зв'язок з особою
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = default!;

    // Контекст дії
    public PlanActionType ActionType { get; set; }      // Dispatch / Return
    public DateTime EventAtUtc { get; set; }            // UTC
    public string Location { get; set; } = default!;
    public string GroupName { get; set; } = default!;
    public string CrewName { get; set; } = default!;

    // Знімок особи на момент дії (історична стабільність)
    public string Rnokpp { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string RankName { get; set; } = default!;
    public string PositionName { get; set; } = default!;
    public string BZVP { get; set; } = default!;
    public string Weapon { get; set; } = default!;
    public string Callsign { get; set; } = default!;
    public string StatusKindOnDate { get; set; } = default!;
}
