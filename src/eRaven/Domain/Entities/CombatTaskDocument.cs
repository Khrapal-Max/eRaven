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
/// Роль в системі:
/// - Контейнер (header) для введення та аудиту.
/// - Має статус Draft/Posted/Canceled.
/// - Містить набір "рядків" (CombatTaskEntry), які введені користувачем.
/// </summary>
public sealed class CombatTaskDocument
{
    public Guid Id { get; set; }

    /// <summary>
    /// Стан документа:
    /// - Draft: редагується
    /// - Posted: зафіксований (джерело аудиту)
    /// - Canceled: відмінений (не застосовується)
    /// </summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    /// <summary>
    /// Номер бойового розпорядження (наказ).
    /// У бізнесі ти вважаєш його унікальним (наприклад включає рік/підрозділ).
    /// </summary>
    public string OrderTitle { get; set; } = string.Empty;

    /// <summary>
    /// Дата документа (дата планування).
    /// </summary>
    public DateOnly RecordedAt { get; set; }

    /// <summary>
    /// Причина скасування (якщо документ Canceled).
    /// </summary>
    public string? CanceledReason { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? CanceledBy { get; set; }
    public DateTime? CanceledAtUtc { get; set; }

    /// <summary>
    /// Рядки документа (денормалізовані записи).
    /// Важливо: тип має бути ICollection/List для EF Core.
    /// </summary>
    public ICollection<CombatTaskEntry> CombatTasks { get; set; } = [];
}