//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTaskSpan
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// Інтервал завдання у табелі (намір/блокування).
/// Єдине джерело правди для "хто/коли на місії" на рівні табеля.
/// </summary>
public sealed class TimesheetTaskSpan
{
    public Guid Id { get; set; }

    public Guid TimesheetId { get; set; }

    /// <summary>
    /// Денормалізований PersonId (щоб уникнути join на читаннях/апдейтах).
    /// Узгоджено з <c>TimeSheetAggregate.PersonId</c>.
    /// </summary>
    public Guid PersonId { get; set; }

    public Guid CombatTaskDocumentId { get; set; }
    public Guid MissionId { get; set; }

    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }

    public DocumentStatus Status { get; set; }

    public Guid? ClosedByCodeId { get; set; }
    public string? ClosedReference { get; set; }

    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// Перевіряє активність інтервалу на дату.
    /// </summary>
    public bool IsActiveOn(DateOnly d)
        => FromDate <= d
           && (!ToDate.HasValue || ToDate.Value >= d)
           && Status != DocumentStatus.Canceled;
}