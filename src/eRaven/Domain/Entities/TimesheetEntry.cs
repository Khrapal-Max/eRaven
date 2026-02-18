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
/// Represents a status in a timeline for half-open range <c>[From..To)</c>.
/// <para>
/// Where:
/// <list type="bullet">
/// <item><description><see cref="From"/> is <b>inclusive</b>.</description></item>
/// <item><description><see cref="To"/> is <b>exclusive</b> (the first day when the entry is no longer active).</description></item>
/// <item><description><c>To == null</c> means “open-ended”.</description></item>
/// </list>
/// </para>
/// <para>
/// Example: a one-day entry for 2026-02-18 is stored as <c>From=2026-02-18</c>, <c>To=2026-02-19</c>.
/// </para>
/// <para>
/// IMPORTANT:
/// - Every entry belongs to a <see cref="TimeSheetAggregate"/>.
/// - PersonId is denormalized for faster reads (must match TimesheetId.PersonId).
/// </para>
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

    /// <summary>
    /// End date (exclusive).
    /// <para><c>null</c> means open-ended.</para>
    /// </summary>
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
