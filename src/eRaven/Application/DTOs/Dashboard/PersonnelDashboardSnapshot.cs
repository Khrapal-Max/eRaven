//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IDashboardRepository
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Dashboard;

/// <summary>
/// “Сирі” дані для Dashboard (read-shape).
/// Репозиторій повертає цифри, handler формує DTO.
/// </summary>
public sealed record PersonnelDashboardSnapshot(
    int TotalInTimesheet,
    int TimesheetUnit,
    int TimesheetOrder,
    int TimesheetBr
);