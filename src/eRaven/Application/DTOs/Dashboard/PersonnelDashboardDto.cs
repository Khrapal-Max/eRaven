//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonnelDashboardDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Dashboard;

public sealed record PersonnelDashboardDto(
    int TotalInTimesheet,
    int TimesheetUnit,
    int TimesheetOrder,
    int TimesheetBr,
    DateTime GeneratedAtUtc
);
