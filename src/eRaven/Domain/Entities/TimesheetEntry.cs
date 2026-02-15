//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//----------------------------------------------------------------------------- 
// TimesheetEntry
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;

namespace eRaven.Domain.Entities;

/// <summary>
/// CRUD timesheet entry.
/// Represents a status in a timeline for inclusive range [From..To].
/// To == null means “open-ended”.
///
/// IMPORTANT:
/// - Every entry belongs to a <see cref="TimeSheetAggregate"/>.
/// - PersonId is denormalized for faster reads (must match TimesheetId.PersonId).
/// </summary>
public sealed class TimesheetEntry
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>FK to owning Timesheet.</summary>
    public Guid TimesheetId { get; set; }

    public TimeSheetAggregate? TimeSheet { get; set; }

    /// <summary>
    /// Denormalized for faster reads (should match Timeline.PersonId).
    /// </summary>
    public Guid PersonId { get; set; }

    /// <summary>
    /// Навігація на статус (довідник табельних кодів).
    /// </summary>
    public Guid TimesheetCodeDefinitionId { get; set; }

    /// <summary>
    /// Навігація на статус (довідник табельних кодів).
    /// </summary>
    public TimesheetCodeDefinition? TimesheetCodeDefinition { get; set; }

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
