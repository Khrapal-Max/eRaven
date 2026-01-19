//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

public interface ITimesheetRepository
{
    Task<Guid> CreateEntryAsync(Guid personId, TimesheetLane lane, string code, DateOnly from, DateOnly? to,
        string? reference, string? note, string author, DateTime nowUtc, CancellationToken ct = default);

    Task UpdateEntryAsync(Guid entryId, TimesheetLane lane, string code, DateOnly from, DateOnly? to,
        string? reference, string? note, string author, DateTime nowUtc, CancellationToken ct = default);

    Task DeleteEntryAsync(Guid entryId, string reason, string author, DateTime nowUtc, CancellationToken ct = default);

    Task<TimesheetEntry?> GetEntryByIdAsync(Guid entryId, CancellationToken ct = default);

    Task<IReadOnlyList<TimesheetEntry>> GetPersonEntriesAsync(Guid personId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>
    /// “Стан на дату” для всіх, хто в табелі на цю дату (через EnrolledAt/ExcludedAt).
    /// Повертає активні записи (по всіх lane). Якщо по lane запису нема — це означає default InArea.
    /// </summary>
    Task<IReadOnlyList<TimesheetEntry>> GetActiveEntriesForTimesheetOnDateAsync(DateOnly date, CancellationToken ct = default);
}
