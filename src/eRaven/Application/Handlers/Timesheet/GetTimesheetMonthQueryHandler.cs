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

public sealed class GetTimesheetMonthQueryHandler(ITimesheetMonthGridRepository repo)
    : IQueryHandler<GetTimesheetMonthQuery, TimesheetMonthGridDto>
{
    private readonly ITimesheetMonthGridRepository _repo = repo;

    public Task<TimesheetMonthGridDto> HandleAsync(GetTimesheetMonthQuery query, CancellationToken ct = default)
        => _repo.GetTimesheetMonthAsync(query.Year, query.Month, query.Search, ct);
}