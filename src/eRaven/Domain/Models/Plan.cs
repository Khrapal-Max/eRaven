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

    /// <summary>Людський номер плану (унікальний). Зберігайте обрізаним.</summary>
    public string PlanNumber { get; set; } = default!;

    public PlanState State { get; set; } = PlanState.Open;

    /// <summary>Автор змін/створення (аудит).</summary>
    public string? Author { get; set; }

    /// <summary>Момент запису в БД (UTC).</summary>
    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;
}
