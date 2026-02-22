//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetEntryQueryRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Application.Abstractions.TimesheetRepository;

/// <summary>
/// Read-only репозиторій для запитів до <see cref="TimesheetEntry"/>.
///
/// <para>Семантика інтервалів:</para>
/// <list type="bullet">
/// <item><description><see cref="TimesheetEntry.From"/> — <b>inclusive</b>.</description></item>
/// <item><description><see cref="TimesheetEntry.To"/> — <b>exclusive</b> (перша дата, коли стан вже не діє).</description></item>
/// <item><description><c>To == null</c> означає “open-ended” (до наступної події).</description></item>
/// </list>
///
/// <para>Soft-delete:</para>
/// події з <see cref="TimesheetEntry.IsDeleted"/> = <c>true</c> ігноруються.
/// </summary>
public interface ITimesheetEntryQueryRepository
{
    //======================================================================
    // Range queries
    //======================================================================

    /// <summary>
    /// Повертає події для багатьох осіб, які перетинаються з діапазоном <c>[from..toExclusive)</c>.
    /// </summary>
    Task<IReadOnlyList<TimesheetEntry>> GetEntriesForPersonsAsync(
        IReadOnlyCollection<Guid> personIds,
        DateOnly from,
        DateOnly toExclusive,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає події для однієї особи, які перетинаються з діапазоном <c>[from..toExclusive)</c>.
    /// </summary>
    Task<IReadOnlyList<TimesheetEntry>> GetEntriesForPersonAsync(
        Guid personId,
        DateOnly from,
        DateOnly toExclusive,
        CancellationToken ct = default);

    //======================================================================
    // Point queries
    //======================================================================

    /// <summary>
    /// Повертає подію за її ідентифікатором або <c>null</c>, якщо подія відсутня/soft-deleted.
    /// </summary>
    Task<TimesheetEntry?> GetByIdAsync(
        Guid entryId,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає активну подію табеля на конкретну дату.
    /// </summary>
    /// <remarks>
    /// Реалізація спочатку знаходить епізод, що покриває дату, а потім повертає подію зі станом
    /// <c>From &lt;= date &amp;&amp; (To == null || date &lt; To)</c>.
    /// </remarks>
    Task<TimesheetEntry?> GetActiveEntryOnDateAsync(
        Guid personId,
        DateOnly date,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає наступну подію (за <see cref="TimesheetEntry.From"/>) після вказаної дати.
    /// </summary>
    Task<TimesheetEntry?> GetNextEntryAfterDateAsync(
        Guid personId,
        DateOnly date,
        CancellationToken ct = default);
}
