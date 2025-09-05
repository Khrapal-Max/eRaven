//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanParticipantSnapshot
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models;

/// <summary>
/// Снапшот людини в складі плану (фіксуємо параметри на момент створення плану).
/// </summary>
public class PlanParticipantSnapshot
{
    public Guid Id { get; set; }

    /// <summary>Посилання на план (FK).</summary>
    public Guid PlanId { get; set; }

    /// <summary>Ідентифікатор людини (на випадок звірки/аналітики).</summary>
    public Guid PersonId { get; set; }

    // --- зафіксовані атрибути на момент плану ---
    public string FullName { get; set; } = default!;
    public string? Rank { get; set; }                     // звання
    public string? PositionSnapshot { get; set; }         // посада/підрозділ «текстом»
    public string? Weapon { get; set; }
    public string? Callsign { get; set; }
    public int? StatusKindId { get; set; }                // поточний статус на момент включення в план (для звіту)
    public string? StatusKindCode { get; set; }           // денорм. код статусу (зручно для історії)

    // службові
    public string? Author { get; set; }
    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;
}