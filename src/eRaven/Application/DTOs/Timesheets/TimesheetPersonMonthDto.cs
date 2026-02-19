//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonMonthDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheets;

/// <summary>
/// Запис для показу місяця для конкретної людини. 
/// Включає в себе інформацію про людину, місяць, кількість днів у місяці,
/// дату останнього оновлення та записи за кожен день місяця.
/// </summary>
public sealed record TimesheetPersonMonthDto(
    TimesheetPersonMonthRowDto Person,
    int Year,
    int Month,
    int DaysInMonth,
    DateTime UpdatedAtUtc,
    IReadOnlyList<TimesheetPersonEntryRowDto> Entries
);