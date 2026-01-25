//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetDayQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Infrastructure.Repositories.TimesheetRepository;

namespace eRaven.Application.Handlers.Timesheet;

public sealed class GetTimesheetDayQueryHandler(
    ITimesheetMonthRepository repo)
    : IQueryHandler<GetTimesheetDayQuery, IReadOnlyList<TimesheetPersonDayRowDto>>
{
    private readonly ITimesheetMonthRepository _repo = repo;

    public async Task<IReadOnlyList<TimesheetPersonDayRowDto>> HandleAsync(
        GetTimesheetDayQuery query,
        CancellationToken ct = default)
    {
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        return await _repo.GetTimesheetDayAsync(query.Date, search, ct);
    }
}