//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDocument
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// Документ планування бойового завдання (наказ/розпорядження).
/// Містить послідовність дій (<see cref="MissionAction"/>) у межах одного документа.
/// </summary>
public sealed class CombatTaskDocument
{
    public Guid Id { get; set; }

    /// <summary>
    /// Стан документа.
    /// </summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    /// <summary>
    /// Номер бойового розпорядження (наказ).
    /// Унікальний у межах доменної домовленості (рік/підрозділ тощо).
    /// </summary>
    public string OrderTitle { get; set; } = string.Empty;

    /// <summary>
    /// Дата документа (коли проведено планування).
    /// </summary>
    public DateOnly RecordedAt { get; set; }

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
