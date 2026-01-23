//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//----------------------------------------------------------------------------- 
// TimesheetEntry
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// CRUD timesheet entry.
/// Represents a status in a lane for inclusive range [From..To].
/// To == null means “open-ended”.
///
/// IMPORTANT: Every entry belongs to a <see cref="TimesheetTimeline"/>.
/// </summary>
public sealed class TimesheetEntry
{
    public Guid Id { get; set; }

    public Guid TimelineId { get; set; }
    public TimesheetTimeline? Timeline { get; set; }

    /// <summary>
    /// Denormalized for faster reads (should match Timeline.PersonId).
    /// </summary>
    public Guid PersonId { get; set; }

    public TimesheetLane Lane { get; set; }

    /// <summary>
    /// Code used in reports/export (e.g. "30", "Ф100", "ПБД" …).
    /// Keep it as string to allow later changes without migrations.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    public DateOnly From { get; set; }
    public DateOnly? To { get; set; }

    public string? Reference { get; set; }
    public string? Note { get; set; }

    // lightweight audit
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    // soft-delete
    public bool IsDeleted { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeleteReason { get; set; }
}
