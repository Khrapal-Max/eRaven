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
    /// <summary>
    /// Повертає таймлайн особи на вказану дату.
    /// </summary>
    Task<TimesheetTimeline?> GetTimelineOnDateAsync(Guid personId, TimesheetLane lane, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// Повертає активний (відкритий) таймлайн особи.
    /// </summary>
    Task<TimesheetTimeline?> GetActiveTimelineAsync(Guid personId, TimesheetLane lane, CancellationToken ct = default);

    /// <summary>
    /// Повертає всі таймлайни, що перетинаються з вказаним періодом.
    /// </summary>
    Task<IReadOnlyList<TimesheetTimeline>> GetTimelinesOverlappingAsync(TimesheetLane lane, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>
    /// Повертає ідентифікатори осіб, у яких є таймлайни, що перетинаються з вказаним періодом.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetPersonIdsOverlappingAsync(TimesheetLane lane, DateOnly from, DateOnly to, CancellationToken ct = default);
}
