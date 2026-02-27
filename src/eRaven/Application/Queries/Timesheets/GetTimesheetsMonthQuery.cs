//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetMonthQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.Timesheets;

/// <summary>
/// Запит на отримання списку епізодв табеля по фільтру рік/місяць
/// </summary>
public sealed record GetTimesheetsMonthQuery(
    int Year,
    int Month,
    string? Search = null);
