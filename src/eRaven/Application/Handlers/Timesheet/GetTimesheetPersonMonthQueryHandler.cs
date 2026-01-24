//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetPersonMonthQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Infrastructure.Repositories.TimesheetRepository;

namespace eRaven.Application.Handlers.Timesheet;

public sealed class GetTimesheetPersonMonthQueryHandler(ITimesheetMonthRepository repo)
    : IQueryHandler<GetTimesheetPersonMonthQuery, TimesheetPersonMonthDto>
{
    private readonly ITimesheetMonthRepository _repo = repo;

    public async Task<TimesheetPersonMonthDto> HandleAsync(GetTimesheetPersonMonthQuery query, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(query.Year, 2000, nameof(query.Year));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(query.Year, 2100, nameof(query.Year));
        ArgumentOutOfRangeException.ThrowIfLessThan(query.Month, 1, nameof(query.Month));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(query.Month, 12, nameof(query.Month));

        var dto = await _repo.GetTimesheetPersonMonthAsync(query.PersonId, query.Year, query.Month, ct);
        return dto ?? throw new InvalidOperationException("Person not found.");
    }
}
