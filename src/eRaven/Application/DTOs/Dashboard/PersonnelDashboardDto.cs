//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonnelDashboardDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Dashboard;

/// <summary>
/// Запис кількості людей по штату, наказу, бойовому розпорядженню
/// </summary>
public sealed record PersonnelDashboardDto(
    int TotalInTimesheet,
    int TimesheetUnit,
    int TimesheetOrder,
    int TimesheetBr,
    DateTime GeneratedAtUtc
);
