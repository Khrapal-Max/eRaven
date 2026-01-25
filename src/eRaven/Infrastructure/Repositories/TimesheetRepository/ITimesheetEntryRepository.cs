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
    /// <summary>
    /// Повертає запис табеля за його унікальним ідентифікатором.
    /// </summary>
    Task<TimesheetEntry?> GetByIdAsync(Guid entryId, CancellationToken ct = default);

    /// <summary>
    /// Повертає записи табеля для вказаної особи у визначеному діапазоні дат.
    /// </summary>
    Task<IReadOnlyList<TimesheetEntry>> GetPersonEntriesAsync(Guid personId, DateOnly from, DateOnly to, TimesheetLane? lane = null, CancellationToken ct = default);

    /// <summary>
    /// Повертає записи табеля, що перетинаються з вказаним діапазоном дат, для багатьох осіб.
    /// </summary>
    Task<IReadOnlyList<TimesheetEntry>> GetEntriesForPersonsAsync(IReadOnlyCollection<Guid> personIds, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>
    /// Повертає активний запис табеля для вказаної особи на певну дату.
    /// </summary>
    Task<TimesheetEntry?> GetActiveEntryOnDateAsync(Guid personId, TimesheetLane lane, DateOnly date, CancellationToken ct = default);

    // CRUD
    Task AddAsync(TimesheetEntry entry, CancellationToken ct = default);
    Task UpdateAsync(TimesheetEntry entry, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid entryId, string reason, string author, DateTime nowUtc, CancellationToken ct = default);
}
