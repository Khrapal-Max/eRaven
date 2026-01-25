//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskPlanLine
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

public sealed class CombatTaskPlanLine
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }
    public CombatTaskPlanDocument Document { get; set; } = null!;

    public CombatTaskPlanLineKind Kind { get; set; }

    public Guid PersonId { get; set; }

    /// <summary>
    /// Один "ланцюжок завдання" для людини.
    /// Для Start генеруємо новий.
    /// Для End - вказуємо який саме Assignment закриваємо.
    /// </summary>
    public Guid AssignmentId { get; set; }

    /// <summary>
    /// Дата дії рядка:
    /// - Start: дата початку виконання
    /// - End: дата закриття виконання
    /// </summary>
    public DateOnly ActionDate { get; set; }

    // -------- Person snapshot (щоб документ "не ламався" при змінах PersonRead) --------
    public string RNOKPP { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Rank { get; set; }
    public string? Position { get; set; }
    public string? Weapon { get; set; }
    public string? Callsign { get; set; }

    // -------- Task details --------
    public string PositionalArea { get; set; } = string.Empty; // позиційний район
    public string GroupName { get; set; } = string.Empty;      // кодова назва (група)
    public string? AssetType { get; set; }                      // тип засобу (може бути null)
    public CombatTaskMode Mode { get; set; }                    // день/ніч/добовий
    public string Goal { get; set; } = string.Empty;            // мета (string select)
    public bool IsActual { get; set; } = true;                  // дійсний чи ні

    // optional notes
    public string? Note { get; set; }
}
