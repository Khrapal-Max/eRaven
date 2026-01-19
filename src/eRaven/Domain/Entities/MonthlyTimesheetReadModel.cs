//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MonthlyTimesheetReadModel
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

/// <summary>
/// Materialized view for monthly timesheet.
/// Stores 31 cells as JSON (1-based day index).
/// Cell should contain Code + EntryId to support safe removals/rebuilds.
/// </summary>
public sealed class MonthlyTimesheetReadModel
{
    public Guid PersonId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    public string DaysJson { get; set; } = "[]";

    public long Version { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
