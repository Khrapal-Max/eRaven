//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionAssignment
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

/// <summary>
/// Проєкційний факт участі людини у місії (Committed-only).
/// <para>
/// Створюється/оновлюється при Posted документа на основі <c>TimesheetTaskSpan</c> зі статусом Posted.
/// </para>
/// <para>
/// Завершення (To/ClosedByDocumentId) може відбутися:
/// </para>
/// <list type="bullet">
/// <item><description>звичайним "закривальним" документом (ClosedByDocumentId = id документа закриття);</description></item>
/// <item><description>аварійним табельним кодом (ClosedByDocumentId = null).</description></item>
/// </list>
/// </summary>
public sealed class MissionAssignment
{
    public Guid Id { get; set; }

    /// <summary>
    /// Документ, який створив факт (джерело старту).
    /// </summary>
    public Guid CombatTaskDocumentId { get; set; }

    public Guid MissionId { get; set; }
    public Guid PersonId { get; set; }

    public DateOnly From { get; set; }
    public DateOnly? To { get; set; }

    /// <summary>
    /// Документ, яким факт був завершений (якщо завершення відбулося штатно документом).
    /// Для аварійних табельних подій (Ф100/200 тощо) значення лишається <c>null</c>.
    /// </summary>
    public Guid? ClosedByDocumentId { get; set; }
}