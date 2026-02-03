//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetMonthRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Read-репозиторій табеля для UI-звітів (місяць/день) та перегляду табеля однієї особи.
///
/// Призначення:
/// - Побудова місячної матриці по всім особам (grid для сторінки /timesheet).
/// - Побудова місячного табеля однієї особи (calendar + список entries).
/// - Побудова денного зрізу.
///
/// Джерело істини:
/// - Дані читаються з <c>TimesheetTimelines</c> та <c>TimesheetEntries</c>.
/// - Для визначення "хто в табелі" використовуємо перетин активного timeline з потрібною датою/місяцем.
/// - Записи <c>TimesheetEntry</c> з <c>IsDeleted = true</c> ігноруються.
///
/// Нотатки щодо UI:
/// - Параметр <paramref name="search"/> застосовується до FullName/RNOKPP (у реалізації).
/// - Реалізація має уникати завантаження проміжних ідентифікаторів у памʼять там, де це можна зробити JOIN-ом.
/// </summary>
public interface ITimesheetMonthRepository
{
    /// <summary>
    /// Повертає місячну матрицю табеля по всім особам за вказаний рік/місяць.
    ///
    /// Результат містить рядки з масивами кодів по днях місяця (наприклад, MainCodes/TaskCodes),
    /// а також додаткові поля для UI (ПІБ, РНОКПП, звання/посада, дати зарахування/виключення тощо).
    ///
    /// Пошук:
    /// - Якщо <paramref name="search"/> заданий, реалізація фільтрує осіб за FullName/RNOKPP.
    /// </summary>
    Task<IReadOnlyList<TimesheetPersonMonthRowDto>> GetTimesheetMonthAsync(
        int year,
        int month,
        string? search,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає місячний табель однієї особи за вказаний рік/місяць.
    ///
    /// Повертає:
    /// - snapshot рядка особи (як у grid),
    /// - перелік entries, що перетинають місяць (source of truth),
    /// - технічні поля для UI (кількість днів у місяці, UpdatedAtUtc).
    ///
    /// Якщо особу не знайдено — повертає <c>null</c>.
    /// </summary>
    Task<TimesheetPersonMonthDto?> GetTimesheetPersonMonthAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає денний зріз табеля на конкретну дату:
    /// для кожної особи, яка "в табелі" на <paramref name="date"/>, повертає стан по lanes (Main/Task).
    ///
    /// Пошук:
    /// - Якщо <paramref name="search"/> заданий, реалізація фільтрує осіб за FullName/RNOKPP.
    /// </summary>
    Task<IReadOnlyList<TimesheetPersonDayRowDto>> GetTimesheetDayAsync(
        DateOnly date,
        string? search,
        CancellationToken ct = default);
}
