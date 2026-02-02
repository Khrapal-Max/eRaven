//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTimeline
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

/// <summary>
/// A lifecycle container for a person's timesheet.
/// One person normally has a single active timeline (ClosedAt == null).
///
/// Timeline визначає “контекст” для записів табеля (TimesheetEntry).
/// </summary>
public sealed class TimesheetTimeline
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Owner person id.</summary>
    public Guid PersonId { get; set; }

    /// <summary>
    /// Date when the timeline was opened (usually the enroll date).
    /// </summary>
    public DateOnly OpenedAt { get; set; }

    /// <summary>
    /// Date when the timeline was closed (inclusive). Null means “active”.
    /// When closed at D, the next day (D+1) is outside the timesheet timeline.
    /// </summary>
    public DateOnly? ClosedAt { get; set; }

    /// <summary>Author who created the timeline.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the timeline was created.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Author who closed the timeline (if any).</summary>
    public string? ClosedBy { get; set; }

    /// <summary>UTC timestamp when the timeline was closed (if any).</summary>
    public DateTime? ClosedAtUtc { get; set; }

    /// <summary>
    /// Navigation to timeline entries.
    /// Optional but useful for EF and clearer mapping.
    /// </summary>
    public ICollection<TimesheetEntry> Entries { get; set; } = [];
}
