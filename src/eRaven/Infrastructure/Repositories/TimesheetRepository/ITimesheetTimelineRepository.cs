//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetTimelineRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Репозиторій таймлайнів табеля (<see cref="TimesheetTimeline"/>).
///
/// Таймлайн — це "контейнер життєвого циклу" табеля для особи:
/// - відкривається в дату <c>OpenedAt</c> (зазвичай при зарахуванні),
/// - закривається в дату <c>ClosedAt</c> (inclusive) або лишається активним, якщо <c>ClosedAt == null</c>.
///
/// Призначення (мінімально необхідне для поточних UI/хендлерів):
/// - знайти таймлайн особи на дату (для читання/валідацій),
/// - знайти активний таймлайн (для команд, що працюють з відкритою шкалою),
/// - знайти всі таймлайни, що перетинають період (для звітів/експорту),
/// - отримати список PersonId, що мають перетин з періодом (для фільтрації вибірки осіб).
///
/// Важливо:
/// - Перетин з періодом визначається як:
///   <c>OpenedAt &lt;= to AND (ClosedAt IS NULL OR ClosedAt &gt;= from)</c>.
/// - Реалізація має бути read-friendly (AsNoTracking) та стабільно детермінована сортуванням
///   (наприклад, <c>OpenedAt DESC, Id DESC</c> для вибору одного).
/// </summary>
public interface ITimesheetTimelineRepository
{
    /// <summary>
    /// Повертає таймлайн особи, який покриває вказану <paramref name="date"/>.
    /// </summary>
    Task<TimesheetTimeline?> GetTimelineOnDateAsync(
        Guid personId,
        DateOnly date,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає активний (відкритий) таймлайн особи, тобто з <c>ClosedAt == null</c>.
    /// Якщо активних декілька (помилка даних) — реалізація має обрати детерміновано (наприклад, latest OpenedAt).
    /// </summary>
    Task<TimesheetTimeline?> GetActiveTimelineAsync(
        Guid personId,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає всі таймлайни, що перетинаються з періодом <paramref name="from"/>.. <paramref name="to"/> (inclusive).
    /// </summary>
    Task<IReadOnlyList<TimesheetTimeline>> GetTimelinesOverlappingAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає унікальний список ідентифікаторів осіб, для яких існує хоча б один таймлайн,
    /// що перетинається з періодом <paramref name="from"/>.. <paramref name="to"/> (inclusive).
    /// </summary>
    Task<IReadOnlyList<Guid>> GetPersonIdsOverlappingAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);
}
