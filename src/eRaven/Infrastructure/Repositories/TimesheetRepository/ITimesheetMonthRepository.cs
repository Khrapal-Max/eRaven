//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetMonthGridRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

public interface ITimesheetMonthRepository
{

    /// <summary>
    /// Повертає місячний звіт табеля з урахуванням фільтру пошуку по імені працівника.
    /// </summary>
    Task<IReadOnlyList<TimesheetPersonMonthRowDto>> GetTimesheetMonthAsync(
        int year,
        int month,
        string? search,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає місячний звіт табеля по конкретній особі.
    /// </summary>
    Task<TimesheetPersonMonthDto?> GetTimesheetPersonMonthAsync(
          Guid personId,
          int year,
          int month,
          CancellationToken ct = default);
}
