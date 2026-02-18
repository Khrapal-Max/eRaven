//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDocument
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// Документ бойових завдань (підстава).
///
/// <para>
/// Спрощена модель:
/// <list type="bullet">
/// <item><description>документ одразу чинний (<see cref="DocumentStatus.Active"/>) і формує факт у табелі;</description></item>
/// <item><description>чернеток/Posted немає;</description></item>
/// <item><description>факт не видаляємо — лише компенсація через <see cref="DocumentStatus.Canceled"/>.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class CombatTaskDocument
{
    public Guid Id { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Active;

    /// <summary>
    /// Назва/заголовок підстави (наприклад: «Наказ №...», «Розпорядження ...»).
    /// </summary>
    public string OrderTitle { get; set; } = string.Empty;

    /// <summary>
    /// Опис/примітка до документа.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Операційна дата документа (для UX/сортування).
    /// </summary>
    public DateOnly RecordedAt { get; set; }

    /// <summary>
    /// Причина скасування (компенсація), якщо документ переведено в <see cref="DocumentStatus.Canceled"/>.
    /// </summary>
    public string? CanceledReason { get; set; }

    //-------------------------------------------------------------------------
    // Audit
    //-------------------------------------------------------------------------

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public string? CanceledBy { get; set; }
    public DateTime? CanceledAtUtc { get; set; }

    //-------------------------------------------------------------------------

    public List<CombatTask> CombatTasks { get; set; } = [];
}
