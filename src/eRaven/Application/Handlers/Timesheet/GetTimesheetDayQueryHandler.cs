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
    ITimesheetRepository repo)
    : IQueryHandler<GetTimesheetDayQuery, IReadOnlyList<TimesheetDayPerPersonCurrentStateDto>>
{
    private readonly ITimesheetRepository _repo = repo;

    public async Task<IReadOnlyList<TimesheetDayPerPersonCurrentStateDto>> HandleAsync(
        GetTimesheetDayQuery query,
        CancellationToken ct = default)
    {
        return await _repo.GetDailyTimesheetAsync(
            date: query.Date,
            search: string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            enrollmentKind: query.EnrollmentKind,
            activeOnly: query.ActiveOnly,
            ct: ct);
    }
}