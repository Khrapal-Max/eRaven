//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonMonthDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheet;

public sealed record TimesheetPersonMonthDto(
    TimesheetPersonMonthRowDto Person,
    int Year,
    int Month,
    int DaysInMonth,
    DateTime UpdatedAtUtc,
    IReadOnlyList<TimesheetPersonEntryRowDto> Entries
);