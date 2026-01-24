//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetMonthGridRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

public interface ITimesheetMonthGridRepository
{

    /// <summary>
    /// Повертає місячний звіт табеля з урахуванням фільтру пошуку по імені працівника.
    /// </summary>
    Task<TimesheetMonthGridDto> GetTimesheetMonthAsync(int year, int month, string? search, CancellationToken ct = default);
}
