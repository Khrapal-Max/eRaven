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
///
/// Роль у системі:
/// - Header + аудит (хто/коли створив/змінив/скасував).
/// - Контейнер для participation-рядків (участь осіб у місіях як інтервали).
/// </summary>
public sealed class CombatTaskDocument
{
    /// <summary>PK документа.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Стан документа:
    /// Draft — редагується,
    /// Posted — зафіксований (джерело аудиту),
    /// Canceled — скасований.
    /// </summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    /// <summary>
    /// Номер/назва бойового розпорядження (наказу).
    /// Рекомендація: зробити унікальним індексом (за потреби з RecordedAt/Unit).
    /// </summary>
    public string OrderTitle { get; set; } = string.Empty;

    /// <summary>Дата документа (дата планування/реєстрації).</summary>
    public DateOnly RecordedAt { get; set; }

    /// <summary>Причина скасування (якщо Status = Canceled).</summary>
    public string? CanceledReason { get; set; }

    /// <summary>Хто створив документ.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Коли створено (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Хто востаннє оновив.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>Коли востаннє оновлено (UTC).</summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>Хто скасував.</summary>
    public string? CanceledBy { get; set; }

    /// <summary>Коли скасовано (UTC).</summary>
    public DateTime? CanceledAtUtc { get; set; }
}
