//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetViewRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository.ReadModels;

namespace eRaven.Application.Abstractions.TimesheetRepository;

/// <summary>
/// Read-репозиторій табеля для UI/звітів.
///
/// <para>Принцип:</para>
/// <list type="bullet">
/// <item><description>репозиторій повертає лише табельні дані (timesheet-only) як готову матрицю днів</description></item>
/// <item><description>дані про особу (ПІБ/РНОКПП/звання/посада) добираються окремо (Person service)</description></item>
/// </list>
/// </summary>
public interface ITimesheetViewRepository
{
    /// <summary>
    /// Повертає матрицю табеля по всім особам на місяць.
    /// </summary>
    Task<IReadOnlyList<TimesheetPeriodRm>> GetTimesheetsMonthAsync(
        int year,
        int month,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає матрицю табеля по всім особам, активним на конкретну дату.
    /// (фактично: період довжиною 1 день)
    /// </summary>
    Task<IReadOnlyList<TimesheetPeriodRm>> GetTimesheetsDayAsync(
        DateOnly date,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає матрицю табеля по всім особам на довільний діапазон дат (inclusive API).
    /// </summary>
    Task<IReadOnlyList<TimesheetPeriodRm>> GetTimesheetsRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає матрицю табеля однієї особи за місяць.
    /// Якщо особу не знайдено у табелі — повертає <c>null</c>.
    /// </summary>
    Task<TimesheetPeriodRm?> GetTimesheetPersonMonthAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken ct = default);
}