//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetTimelineRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Репозиторій епізодів табеля (aggregate root) (<see cref="TimeSheetAggregate"/>).
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
    Task<TimeSheetAggregate?> GetTimelineOnDateAsync(
        Guid personId,
        DateOnly date,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає активний (відкритий) таймлайн особи, тобто з <c>ClosedAt == null</c>.
    /// Якщо активних декілька (помилка даних) — реалізація має обрати детерміновано (наприклад, latest OpenedAt).
    /// </summary>
    Task<TimeSheetAggregate?> GetActiveTimelineAsync(
        Guid personId,
        CancellationToken ct = default);

    // NOTE: overlapping/personIds тримаємо в read-репозиторіях (Month/Range), щоб не змішувати CQRS.
}
