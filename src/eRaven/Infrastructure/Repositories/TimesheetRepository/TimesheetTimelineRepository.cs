//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTimelineRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

public sealed class TimesheetTimelineRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetTimelineRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<TimesheetTimeline?> GetTimelineOnDateAsync(Guid personId, DateOnly date, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetTimelines
            .AsNoTracking()
            .Where(x => x.PersonId == personId)
            .Where(x => x.OpenedAt <= date && (!x.ClosedAt.HasValue || x.ClosedAt.Value >= date))
            .OrderByDescending(x => x.OpenedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<TimesheetTimeline?> GetActiveTimelineAsync(Guid personId, CancellationToken ct = default)
    {
        //TODO не задіяний
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetTimelines
            .AsNoTracking()
            .Where(x => x.PersonId == personId && x.ClosedAt == null)
            .OrderByDescending(x => x.OpenedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<TimesheetTimeline>> GetTimelinesOverlappingAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        //TODO не задіяний
        if (to < from) throw new ArgumentException("To must be >= From.", nameof(to));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetTimelines
            .AsNoTracking()
            .Where(x => x.OpenedAt <= to && (!x.ClosedAt.HasValue || x.ClosedAt.Value >= from))
            .OrderBy(x => x.PersonId)
            .ThenBy(x => x.OpenedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> GetPersonIdsOverlappingAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        //TODO не задіяний
        if (to < from) throw new ArgumentException("To must be >= From.", nameof(to));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetTimelines
            .AsNoTracking()
            .Where(x => x.OpenedAt <= to && (!x.ClosedAt.HasValue || x.ClosedAt.Value >= from))
            .Select(x => x.PersonId)
            .Distinct()
            .ToListAsync(ct);
    }
}
