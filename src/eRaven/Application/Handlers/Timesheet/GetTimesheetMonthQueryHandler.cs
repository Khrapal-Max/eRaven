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

public sealed class GetTimesheetMonthQueryHandler(ITimesheetMonthRepository repo)
    : IQueryHandler<GetTimesheetMonthQuery, IReadOnlyList<TimesheetPersonMonthRowDto>>
{
    private readonly ITimesheetMonthRepository _repo = repo;

    public Task<IReadOnlyList<TimesheetPersonMonthRowDto>> HandleAsync(GetTimesheetMonthQuery query, CancellationToken ct = default)
        => _repo.GetTimesheetMonthAsync(query.Year, query.Month, query.Search, ct);
}