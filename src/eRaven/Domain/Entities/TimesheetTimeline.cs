//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTimeline
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// A lifecycle container for a person's timesheet lane.
/// One enrollment period normally corresponds to one active timeline per lane.
/// </summary>
public sealed class TimesheetTimeline
{
    public Guid Id { get; set; }

    public Guid PersonId { get; set; }
    public TimesheetLane Lane { get; set; }

    /// <summary>
    /// Date when the timeline was opened (usually the enroll date).
    /// </summary>
    public DateOnly OpenedAt { get; set; }

    /// <summary>
    /// Date when the timeline was closed (inclusive). Null means “active”.
    /// When closed at D, the next day (D+1) is outside the timesheet (NB).
    /// </summary>
    public DateOnly? ClosedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public string? ClosedBy { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
}
