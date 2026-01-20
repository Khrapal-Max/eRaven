//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MonthlyTimesheetDay
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// One cell in monthly timesheet (1-based day index) for a specific lane.
/// </summary>
public sealed record MonthlyTimesheetDay
{
    public Guid? EntryId { get; set; }           // щоб коректно зв’язати з TimesheetEntry
    public int Day { get; set; }                 // 1..31 (реально буде 28/29/30/31)
    public TimesheetLane Lane { get; set; }      // Main / Task
    public string Code { get; set; } = string.Empty;       // "30", "0", "100", "ВП"...   
}
