//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetStatusCatalog
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Catalogs.Timesheet;

public interface ITimesheetStatusCatalog
{
    IReadOnlyList<TimesheetStatusOption> GetAll();
    IReadOnlyList<TimesheetStatusOption> GetByLane(TimesheetLane lane);
}
