//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskAssignment
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// Поточний стан завдання по особі.
/// Створюється/оновлюється тільки коли документ стає Posted.
/// </summary>
public sealed class CombatTaskAssignment
{
    /// <summary>= AssignmentId.</summary>
    public Guid Id { get; set; }

    public Guid PersonId { get; set; }

    /// <summary>Дата початку виконання.</summary>
    public DateOnly StartedAt { get; set; }

    /// <summary>
    /// Дата закриття. NULL => завдання активне (відкрите).
    /// ВАЖЛИВО: нове Start заборонено, поки EndedAt = NULL.
    /// </summary>
    public DateOnly? EndedAt { get; set; }

    /// <summary>Документ, який відкрив це завдання (рядок Start).</summary>
    public Guid StartDocumentId { get; set; }

    /// <summary>Документ, який закрив це завдання (рядок End).</summary>
    public Guid? EndDocumentId { get; set; }

    // Дублюємо потрібні деталі для денних/місячних вибірок (без джойну до Lines)
    public DateOnly PlanningDate { get; set; }
    public string PlanningDocTitle { get; set; } = string.Empty;

    public string PositionalArea { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string? AssetType { get; set; }
    public CombatTaskMode Mode { get; set; }
    public string Goal { get; set; } = string.Empty;
    public bool IsActual { get; set; } = true;

    // Person snapshot (опційно, але зручно для денних реєстрів)
    public string RNOKPP { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Rank { get; set; }
    public string? Position { get; set; }
    public string? Weapon { get; set; }
    public string? Callsign { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
