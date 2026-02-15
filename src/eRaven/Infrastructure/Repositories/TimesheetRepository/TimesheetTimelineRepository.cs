//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTimelineRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

public sealed class TimesheetTimelineRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetTimelineRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <inheritdoc />
    public async Task<TimeSheetAggregate?> GetTimelineOnDateAsync(Guid personId, DateOnly date, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimeSheets
            .AsNoTracking()
            .Where(x => x.PersonId == personId)
            .Where(x => x.OpenedAt <= date && (!x.ClosedAt.HasValue || x.ClosedAt.Value >= date))
            .OrderByDescending(x => x.OpenedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<TimeSheetAggregate?> GetActiveTimelineAsync(Guid personId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimeSheets
            .AsNoTracking()
            .Where(x => x.PersonId == personId && x.ClosedAt == null)
            .OrderByDescending(x => x.OpenedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }
}
