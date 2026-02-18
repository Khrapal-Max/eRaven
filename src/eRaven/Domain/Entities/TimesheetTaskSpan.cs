//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTaskSpan
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// Інтервал призначення на місію/завдання у табелі (факт).
/// Єдине джерело правди для "хто/коли на місії" на рівні табеля.
///
/// <para>
/// Семантика інтервалу:
/// <list type="bullet">
/// <item><description><see cref="FromDate"/> — inclusive (включно).</description></item>
/// <item><description><see cref="ToDate"/> — EXCLUSIVE (невключно). Null = без верхньої межі.</description></item>
/// <item><description>Тобто інтервал: <c>[FromDate..ToDate)</c>.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class TimesheetTaskSpan
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>FK to owning timesheet episode.</summary>
    public Guid TimesheetId { get; set; }

    /// <summary>Denormalized PersonId (must match episode.PersonId).</summary>
    public Guid PersonId { get; set; }

    /// <summary>Mission/Task id.</summary>
    public Guid MissionId { get; set; }

    /// <summary>Document that opened the span.</summary>
    public Guid OpenedByCombatTaskDocumentId { get; set; }

    /// <summary>Document that closed/canceled the span (if any).</summary>
    public Guid? ClosedByCombatTaskDocumentId { get; set; }

    /// <summary>Start date (inclusive).</summary>
    public DateOnly FromDate { get; set; }

    /// <summary>
    /// End date (EXCLUSIVE). Null means open-ended (still active).
    /// </summary>
    public DateOnly? ToDate { get; set; }

    /// <summary>Status of the fact (Active/Canceled).</summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.Active;

    // Snapshot
    public string Rnokpp { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public string? Rank { get; set; }
    public string? Position { get; set; }
    public string? Weapon { get; set; }
    public string? Callsign { get; set; }

    // Close metadata
    public Guid? ClosedByCodeId { get; set; }
    public string? ClosedReference { get; set; }

    // Audit
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// Перевіряє активність інтервалу на дату <paramref name="d"/>.
    /// Інтервал half-open: <c>[FromDate..ToDate)</c>.
    /// </summary>
    public bool IsActiveOn(DateOnly d)
        => Status != DocumentStatus.Canceled
           && FromDate <= d
           && (!ToDate.HasValue || d < ToDate.Value);
}
