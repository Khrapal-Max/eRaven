//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetMonthQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.Timesheets;

public sealed record GetTimesheetMonthQuery(
    int Year,
    int Month,
    string? Search = null);
