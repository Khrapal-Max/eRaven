//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetPersonMonthQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.Timesheets;

public sealed record GetTimesheetPersonMonthQuery(
    Guid PersonId,
    int Year,
    int Month
);
