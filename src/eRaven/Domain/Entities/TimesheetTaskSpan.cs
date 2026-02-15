//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTaskSpan
//-----------------------------------------------------------------------------


using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// Сутність агрегата
/// </summary>
public sealed class TimesheetTaskSpan
{
    public Guid Id { get; set; }

    // aggregate root ref
    public Guid TimesheetId { get; set; }

    public Guid CombatTaskDocumentId { get; set; }

    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }

    public DocumentStatus Status { get; set; }

    // Опційно: як саме “закрили” контекст (Ф100/200/ПБД і т.д.)
    public Guid? ClosedByCodeId { get; set; }
    public string? ClosedReference { get; set; }

    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }

    public bool IsActiveOn(DateOnly d)
        => FromDate <= d && (!ToDate.HasValue || ToDate.Value >= d) && Status != DocumentStatus.Canceled;
}
