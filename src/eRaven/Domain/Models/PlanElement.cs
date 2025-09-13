//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanElement
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Models;

/// <summary>
/// Окремий «рядок» плану: дія над основною особою + (за потреби) супутні учасники.
/// </summary>
public class PlanElement
{
    public Guid Id { get; set; }

    /// <summary>FK на план.</summary>
    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    // ---- індивідуальна дія та її час ----
    public PlanType Type { get; set; }          // Відрядити / Повернути
    public DateTime EventAtUtc { get; set; }    // Дата події (UTC, бажано 00/15/30/45)

    // ---- атрибути події (контекст) ----
    public string? Location { get; set; }
    public string? GroupName { get; set; }
    public string? ToolType { get; set; }

    public Guid GuidParticipantId { get; set; }
    public PlanParticipantSnapshot PlanParticipantSnapshot { get; set; } = default!; // основна особа

    // ---- службові поля ----
    public string? Note { get; set; }
    public string? Author { get; set; }
    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;

    // --------- хелпер узгодженості часу ---------
    public static bool IsQuarterAligned(DateTime dtUtc)
        => dtUtc.Minute % 15 == 0 && dtUtc.Second == 0 && dtUtc.Millisecond == 0;

    /// <summary>Валідатор для бізнес-інваріанта часу у 15-хв інтервалах.</summary>
    public void EnsureQuarterAligned()
    {
        if (!IsQuarterAligned(EventAtUtc))
            throw new InvalidOperationException("Час події має бути на інтервалах 00/15/30/45 хв без секунд.");
    }
}