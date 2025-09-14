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

    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public Guid PersonId { get; set; }          // 🔴 денормалізовано для індексів/гвардів
    public PlanType Type { get; set; }
    public DateTime EventAtUtc { get; set; }    // 00/15/30/45

    public string? Location { get; set; }
    public string? GroupName { get; set; }
    public string? ToolType { get; set; }

    public string? Note { get; set; }
    public string? Author { get; set; }
    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;

    public PlanParticipantSnapshot PlanParticipantSnapshot { get; set; } = null!;

    public static bool IsQuarterAligned(DateTime dtUtc)
        => dtUtc.Minute % 15 == 0 && dtUtc.Second == 0 && dtUtc.Millisecond == 0;

    public void EnsureQuarterAligned()
    {
        if (!IsQuarterAligned(EventAtUtc))
            throw new InvalidOperationException("Час має бути 00/15/30/45 без секунд.");
    }
}