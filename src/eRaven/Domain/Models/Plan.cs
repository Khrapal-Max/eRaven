//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Plan
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Models;

/// <summary>
/// План разової дії. Закривається наказом (State: Open → Close).
/// </summary>
public class Plan
{
    public Guid Id { get; set; }

    /// <summary>Людський номер плану (унікальний, для документів/звітів).</summary>
    public string PlanNumber { get; set; } = default!;

    /// <summary>Стан плану: відкритий/закритий (наказом).</summary>
    public PlanState State { get; set; } = PlanState.Open;

    // 🔵 Багато планів можуть бути закриті ОДНИМ наказом (1:N)
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }

    /// <summary>Автор створення/змін (аудит).</summary>
    public string? Author { get; set; }

    /// <summary>Момент створення (UTC).</summary>
    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Учасники/дії в межах плану (чернетка до публікації).</summary>
    public ICollection<PlanAction> PlanActions { get; set; } = [];
}
