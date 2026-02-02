//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//----------------------------------------------------------------------------- 
// TimesheetEntry
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

/// <summary>
/// CRUD timesheet entry.
/// Represents a status in a timeline for inclusive range [From..To].
/// To == null means “open-ended”.
///
/// IMPORTANT:
/// - Every entry belongs to a <see cref="TimesheetTimeline"/>.
/// - PersonId is denormalized for faster reads (must match Timeline.PersonId).
/// </summary>
public sealed class TimesheetEntry
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>FK to owning timeline.</summary>
    public Guid TimelineId { get; set; }

    public TimesheetTimeline? Timeline { get; set; }

    /// <summary>
    /// Denormalized for faster reads (should match Timeline.PersonId).
    /// </summary>
    public Guid PersonId { get; set; }

    /// <summary>
    /// Code used in reports/export (e.g. "30", "Ф100", "ПБД" …).
    /// Keep it as string to allow later changes without migrations.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Start date (inclusive).</summary>
    public DateOnly From { get; set; }

    /// <summary>End date (inclusive). Null means open-ended.</summary>
    public DateOnly? To { get; set; }

    /// <summary>Optional reference (document number, order id, etc.).</summary>
    public string? Reference { get; set; }

    /// <summary>Optional note.</summary>
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
