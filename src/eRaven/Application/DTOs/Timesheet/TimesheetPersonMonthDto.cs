//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonMonthDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.Timesheet;

public sealed record TimesheetPersonMonthDto(
    Guid PersonId,
    string FullName,
    string RNOKPP,
    string? Rank,
    string? Position,
    EnrollmentKind? EnrollmentKind,

    int Year,
    int Month,
    int DaysInMonth,

    MonthlyTimesheetReadModelDto Timesheet,
    IReadOnlyList<TimesheetEntryDto> Entries
);
