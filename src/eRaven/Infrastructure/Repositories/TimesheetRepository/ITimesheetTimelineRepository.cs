//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetTimelineRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

public interface ITimesheetTimelineRepository
{
    // Active timeline (covers date) for person+lane
    Task<TimesheetTimeline?> GetTimelineOnDateAsync(Guid personId, TimesheetLane lane, DateOnly date, CancellationToken ct = default);

    // Active timeline regardless of date (ClosedAt == null)
    Task<TimesheetTimeline?> GetActiveTimelineAsync(Guid personId, TimesheetLane lane, CancellationToken ct = default);

    // Timelines overlapping a period [from..to] (inclusive)
    Task<IReadOnlyList<TimesheetTimeline>> GetTimelinesOverlappingAsync(TimesheetLane lane, DateOnly from, DateOnly to, CancellationToken ct = default);

    // Convenience: person ids in timesheet for a date/period (Main lane usually)
    Task<IReadOnlyList<Guid>> GetPersonIdsOverlappingAsync(TimesheetLane lane, DateOnly from, DateOnly to, CancellationToken ct = default);
}
