//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

public interface ITimesheetRepository
{
    // Отримати місячний табель
    Task<IReadOnlyList<TimesheetMonthPerPersonDto>> GetMonthlyTimesheetAsync(int year, int month, string? search, CancellationToken ct = default);

    // Отримати денний табель
    Task<IReadOnlyList<TimesheetDayPerPersonCurrentStateDto>> GetDailyTimesheetAsync(
       DateOnly date,
       string? search,
       EnrollmentKind? enrollmentKind = null,
       bool activeOnly = true,
       CancellationToken ct = default);

    // Отримати всі записи табеля певної особи за період
    Task<IReadOnlyList<TimesheetEntry>> GetPersonEntriesAsync(Guid personId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>
    /// “Стан на дату” для всіх, хто в табелі на цю дату (через EnrolledAt/ExcludedAt).
    /// Повертає активні записи (по всіх lane). Якщо по lane запису нема — це означає default InArea.
    /// </summary>
    Task<IReadOnlyList<TimesheetEntry>> GetActiveEntriesForTimesheetOnDateAsync(DateOnly date, CancellationToken ct = default);

    // Отримати запис табеля за Id
    Task<TimesheetEntry?> GetEntryByIdAsync(Guid entryId, CancellationToken ct = default);

    // “Відкрити табель” при зарахуванні: мінімально створити запис InArea Main=30 з дати зарахування
    Task EnsureOpenedOnEnrollAsync(Guid personId, DateOnly enrollDate, string author,
        DateTime nowUtc, CancellationToken ct = default);

    // “Закрити табель” при виключенні:
    Task EnsureClosedOnExcludeAsync(Guid personId, DateOnly closeTo, string? reason, string author,
        DateTime nowUtc, CancellationToken ct = default);

    // CRUD операції з записами табеля:
    Task<Guid> CreateEntryAsync(Guid personId, TimesheetLane lane, string code, DateOnly from, DateOnly? to,
        string? reference, string? note, string author, DateTime nowUtc, CancellationToken ct = default);

    Task UpdateEntryAsync(Guid entryId, TimesheetLane lane, string code, DateOnly from, DateOnly? to,
        string? reference, string? note, string author, DateTime nowUtc, CancellationToken ct = default);

    Task DeleteEntryAsync(Guid entryId, string reason, string author, DateTime nowUtc, CancellationToken ct = default);
}
