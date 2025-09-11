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

    /// <summary>FK на елемент плану.</summary>
    public Guid PlanElementId { get; set; }
    public PlanElement PlanElement { get; set; } = null!;

    /// <summary>Ідентифікатор людини (для звірки/аналітики).</summary>
    public Guid PersonId { get; set; }

    // --- зафіксовані атрибути на момент включення ---
    public string FullName { get; set; } = default!;
    public string Rnokpp { get; set; } = default!;       // обов’язково, див. конфіг: not blank
    public string? Rank { get; set; }
    public string? PositionSnapshot { get; set; }
    public string? Weapon { get; set; }
    public string? Callsign { get; set; }
    public int? StatusKindId { get; set; }
    public string? StatusKindCode { get; set; }

    // службові
    public string? Author { get; set; }
    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;
}
