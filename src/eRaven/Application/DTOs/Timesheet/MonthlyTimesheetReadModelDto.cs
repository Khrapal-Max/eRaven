//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MonthlyTimesheetReadModelDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheet;

public sealed record MonthlyTimesheetReadModelDto
(
    Guid PersonId,
    int Year,
    int Month,
    DateTime UpdatedAtUtc,
    IReadOnlyList<MonthlyTimesheetDayDto> Days
);
