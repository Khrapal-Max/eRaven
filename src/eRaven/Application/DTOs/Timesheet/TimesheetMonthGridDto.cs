//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetMonthGridDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheet;

/// <summary>
/// DTO для візуального “місячного табеля” (матриця по всім особам).
/// Для UI: замість списку “Days”, тут одразу масиви кодів по днях.
/// </summary>
public sealed record TimesheetMonthGridDto(
    int Year,
    int Month,
    int DaysInMonth,
    IReadOnlyList<TimesheetMonthPersonRowDto> Rows);
