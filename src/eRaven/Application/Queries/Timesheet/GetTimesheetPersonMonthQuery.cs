//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetPersonMonthQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.Timesheet;

public sealed record GetTimesheetPersonMonthQuery(
    Guid PersonId,
    int Year,
    int Month
);
