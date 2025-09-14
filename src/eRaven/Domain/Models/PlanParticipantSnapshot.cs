//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanParticipantSnapshot
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models;

/// <summary>
/// Снапшот людини в складі елемента плану (фіксуємо параметри на момент створення).
/// </summary>
public class PlanParticipantSnapshot
{
    public Guid Id { get; set; }
    public Guid PlanElementId { get; set; }
    public PlanElement PlanElement { get; set; } = null!;

    public Guid PersonId { get; set; }

    public string FullName { get; set; } = default!;
    public string Rnokpp { get; set; } = default!;
    public string? Rank { get; set; }
    public string? PositionSnapshot { get; set; }
    public string? Weapon { get; set; }
    public string? Callsign { get; set; }
    public int? StatusKindId { get; set; }
    public string? StatusKindCode { get; set; }

    public string? Author { get; set; }
    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;
}
