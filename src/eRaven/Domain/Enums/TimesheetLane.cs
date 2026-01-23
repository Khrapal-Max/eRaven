//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetLane
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Enums;

/// <summary>
/// Logical “lanes” in timesheet timeline.
/// Each lane may have only one active entry at a time (no overlaps).
/// </summary>
public enum TimesheetLane
{
    Main = 0, // звичайні табельні стани (район/відпустка/лікування/відрядження/НБ тощо)
    Task = 1  // task lane (рапорт/БР/БТГр/Ф100/ПБД тощо)
}