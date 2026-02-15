//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetAggregateRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// EF Core реалізація <see cref="ITimesheetAggregateRepository"/>.
/// </summary>
public sealed class TimesheetAggregateRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetAggregateRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    private AppDbContext? _db;

    /// <inheritdoc />
    public async Task<TimeSheetAggregate?> LoadActiveForUpdateAsync(Guid personId, CancellationToken ct = default)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        _db ??= await _dbFactory.CreateDbContextAsync(ct);

        return await _db.TimeSheets
            .Include(x => x.Entries)
            .Include(x => x.TaskSpans)
            .Where(x => x.PersonId == personId && x.ClosedAt == null)
            .OrderByDescending(x => x.OpenedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<TimeSheetAggregate?> LoadOnDateForUpdateAsync(Guid personId, DateOnly date, CancellationToken ct = default)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        _db ??= await _dbFactory.CreateDbContextAsync(ct);

        return await _db.TimeSheets
            .Include(x => x.Entries)
            .Include(x => x.TaskSpans)
            .Where(x => x.PersonId == personId)
            .Where(x => x.OpenedAt <= date && (!x.ClosedAt.HasValue || x.ClosedAt.Value >= date))
            .OrderByDescending(x => x.OpenedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task SaveAsync(CancellationToken ct = default)
    {
        if (_db is null)
            return;

        await _db.SaveChangesAsync(ct);
    }
}
