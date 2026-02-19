//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExportTimesheetMonthQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.Timesheets;

public sealed record ExportTimesheetMonthQuery(
    int Year,
    int Month,
    string? Search = null
);