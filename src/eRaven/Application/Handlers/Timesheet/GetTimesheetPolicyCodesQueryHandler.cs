//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetPolicyCodesQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;

namespace eRaven.Application.Handlers.Timesheet;

public sealed class GetTimesheetPolicyCodesQueryHandler(
    ITimesheetPolicyRepository repo)
    : IQueryHandler<GetTimesheetPolicyCodesQuery, IReadOnlyList<TimesheetCodeDto>>
{
    private readonly ITimesheetPolicyRepository _repo = repo;

    public async Task<IReadOnlyList<TimesheetCodeDto>> HandleAsync(
        GetTimesheetPolicyCodesQuery query,
        CancellationToken ct = default)
    {
        var codes = await _repo.GetCodesAsync(query.IncludeInactive, ct);

        return [.. codes
            .Select(c => new TimesheetCodeDto(
                c.Id,
                c.Code,
                c.Title,
                c.Description,
                c.SortOrder,
                c.Priority,
                c.IsTerminal,
                c.IsActive))];
    }
}