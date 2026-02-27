//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetEntryWriterRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Application.Abstractions.TimesheetRepository;

/// <summary>
/// Write-only репозиторій подій табеля.
///
/// <para>
/// ВАЖЛИВО: будь-які зміни подій (<see cref="TimesheetEntry"/>) виконуються <b>тільки</b>
/// через методи агрегату епізоду (<c>TimeSheetAggregate</c>).
/// Репозиторій не має "керувати" інтервалами напряму (наприклад, виставляти <see cref="TimesheetEntry.To"/>).
/// </para>
///
/// <para>
/// Потік/оркестрація (manual/enroll/exclude/task) залишається зовнішнім:
/// цей контракт лише застосовує конкретні зміни в межах одного виклику.
/// </para>
/// </summary>
public interface ITimesheetEntryWriterRepository
{
    //======================================================================
    // Domain-level operations (recommended)
    //======================================================================

    /// <summary>
    /// Додає подію в епізод табеля на дату <paramref name="effectiveAt"/>.
    /// </summary>
    /// <returns>Ідентифікатор створеної події.</returns>
    Task<Guid> AddEntryAsync(
        Guid personId,
        DateOnly effectiveAt,
        Guid codeId,
        string? reference,
        string? note,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Коригує існуючу подію епізоду табеля.
    /// </summary>
    Task CorrectEntryAsync(
        Guid personId,
        Guid entryId,
        Guid nextCodeId,
        DateOnly nextEffectiveAt,
        string? reference,
        string? note,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Видаляє подію епізоду табеля (фізично з колекції епізоду).
    /// </summary>
    Task RemoveEntryAsync(
        Guid personId,
        Guid entryId,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Перехід стану на дату <paramref name="effectiveAt"/>.
    /// <para>
    /// Якщо на цю дату вже існує подія (тобто <c>active.From == effectiveAt</c>) — виконується replace-in-place,
    /// інакше додається нова подія.
    /// </para>
    /// </summary>
    Task<Guid> TransitionAsync(
        Guid personId,
        DateOnly effectiveAt,
        Guid nextCodeId,
        string? reference,
        string? note,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Застосовує набір точок перемикання (facts/points) для конкретної особи.
    /// <para>
    /// Використовується для інтеграції з CombatTask: CombatTask обчислює точки, Timesheet лише записує події.
    /// </para>
    /// </summary>
    Task ApplyChangePointsAsync(
        Guid personId,
        IReadOnlyList<(DateOnly EffectiveAt, Guid CodeId, string? Reference)> points,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    //======================================================================
    // Legacy / low-level operations (keep only if still used)
    //======================================================================

    /// <summary>
    /// Soft-delete події.
    /// <para>
    /// Рекомендація: нові сценарії мають використовувати доменну операцію в writer (через агрегат).
    /// Цей метод залишено для сумісності зі старими хендлерами.
    /// </para>
    /// </summary>
    Task SoftDeleteAsync(
        Guid entryId,
        string reason,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Оновлює існуючу подію (in-place).
    /// <para>
    /// Метод очікує, що audit-поля (<see cref="TimesheetEntry.UpdatedBy"/>, <see cref="TimesheetEntry.UpdatedAtUtc"/>)
    /// вже заповнені у <paramref name="updated"/>.
    /// </para>
    /// </summary>
    Task UpdateAsync(
        TimesheetEntry updated,
        CancellationToken ct = default);

    /// <summary>
    /// Атомарна операція transition у вигляді (prevUpdated + nextAdded).
    /// <para>
    /// Метод очікує, що audit-поля в обох об'єктах заповнені:
    /// <list type="bullet">
    /// <item><description><paramref name="prevUpdated"/>: UpdatedBy/UpdatedAtUtc</description></item>
    /// <item><description><paramref name="nextAdded"/>: CreatedBy/CreatedAtUtc</description></item>
    /// </list>
    /// </para>
    /// </summary>
    Task SaveTransitionAsync(
        TimesheetEntry prevUpdated,
        TimesheetEntry nextAdded,
        CancellationToken ct = default);
}
