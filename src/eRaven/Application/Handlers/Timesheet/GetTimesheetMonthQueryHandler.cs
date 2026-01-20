//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetMonthQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Infrastructure.Repositories.TimesheetRepository;

namespace eRaven.Application.Handlers.Timesheet;

public sealed class GetTimesheetMonthQueryHandler(
    ITimesheetRepository repo)
    : IQueryHandler<GetTimesheetMonthQuery, IReadOnlyList<TimesheetMonthPerPersonDto>>
{
    private readonly ITimesheetRepository _repo = repo;

    public async Task<IReadOnlyList<TimesheetMonthPerPersonDto>> HandleAsync(
        GetTimesheetMonthQuery query,
        CancellationToken ct = default)
    {
        return await _repo.GetMonthlyTimesheetAsync(
            year: query.Year,
            month: query.Month,
            search: query.Search,
            ct: ct);
    }
}