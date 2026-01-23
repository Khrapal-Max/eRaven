//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetEntryRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

public interface ITimesheetEntryRepository
{
    Task<TimesheetEntry?> GetByIdAsync(Guid entryId, CancellationToken ct = default);

    // Entries overlapping [from..to] for person (optionally lane)
    Task<IReadOnlyList<TimesheetEntry>> GetPersonEntriesAsync(Guid personId, DateOnly from, DateOnly to, TimesheetLane? lane = null, CancellationToken ct = default);

    // Entries overlapping [from..to] for many persons (for month/day queries)
    Task<IReadOnlyList<TimesheetEntry>> GetEntriesForPersonsAsync(IReadOnlyCollection<Guid> personIds, DateOnly from, DateOnly to, CancellationToken ct = default);

    // Active entry on date for person+lane (To null = open)
    Task<TimesheetEntry?> GetActiveEntryOnDateAsync(Guid personId, TimesheetLane lane, DateOnly date, CancellationToken ct = default);

    // CRUD
    Task AddAsync(TimesheetEntry entry, CancellationToken ct = default);
    Task UpdateAsync(TimesheetEntry entry, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid entryId, string reason, string author, DateTime nowUtc, CancellationToken ct = default);
}
