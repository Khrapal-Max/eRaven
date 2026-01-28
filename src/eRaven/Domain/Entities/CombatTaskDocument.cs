//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDocument
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// Документ планування бойового завдання.
/// - DocumentTitle: Назва/номер документа планування.
/// - RecordedAt: Дата документа (коли проведено планування).
/// - Status: Стан документа.
/// - CanceledReason: Причина скасування документа (якщо застосовно).
/// </summary>
public sealed class CombatTaskDocument
{
    public Guid Id { get; set; }

    /// <summary>
    /// Назва/номер документа планування.
    /// </summary>
    public string DocumentTitle { get; set; } = string.Empty;

    /// <summary>
    /// Стан документа.
    /// </summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    /// <summary>
    /// Дата документа (коли проведено планування).
    /// </summary>
    public DateOnly RecordedAt { get; set; }

    /// <summary>
    /// Номер бойового розпорядження (документ проведений).
    /// </summary>
    public string? Order { get; set; }


    /// <summary>
    /// Причина скасування документа.
    /// </summary>
    public string? CanceledReason { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? CanceledBy { get; set; }
    public DateTime? CanceledAtUtc { get; set; }
}
