//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetViewRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;

namespace eRaven.Application.Abstractions.TimesheetRepository;

/// <summary>
/// Read-репозиторій табеля для UI/звітів.
///
/// <para>Фокус:</para>
/// <list type="bullet">
/// <item><description>місячна матриця (grid) по всім особам</description></item>
/// <item><description>довільний діапазон дат (операційні вʼюхи)</description></item>
/// <item><description>персональний місячний табель (calendar + entries)</description></item>
/// <item><description>денний зріз (стан на дату)</description></item>
/// </list>
///
/// <para>Джерело істини:</para>
/// <list type="bullet">
/// <item><description><c>TimeSheets</c> (епізоди) + <c>TimesheetEntries</c> (факти)</description></item>
/// <item><description>soft-deleted записи (<c>IsDeleted</c>) ігноруються</description></item>
/// </list>
/// </summary>
public interface ITimesheetViewRepository
{
    /// <summary>
    /// Повертає місячну матрицю табеля по всім особам за вказаний рік/місяць.
    /// </summary>
    Task<IReadOnlyList<TimesheetPersonMonthRowDto>> GetTimesheetMonthAsync(
        int year,
        int month,
        string? search,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає матрицю табеля по всім особам за довільний діапазон дат (inclusive).
    /// </summary>
    Task<IReadOnlyList<TimesheetPersonRangeRowDto>> GetTimesheetRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        string? search,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає місячний табель однієї особи за вказаний рік/місяць.
    /// Якщо особу не знайдено — повертає <c>null</c>.
    /// </summary>
    Task<TimesheetPersonMonthDto?> GetTimesheetPersonMonthAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає денний зріз табеля на конкретну дату.
    /// </summary>
    Task<IReadOnlyList<TimesheetPersonDayRowDto>> GetTimesheetDayAsync(
        DateOnly date,
        string? search,
        CancellationToken ct = default);
}
