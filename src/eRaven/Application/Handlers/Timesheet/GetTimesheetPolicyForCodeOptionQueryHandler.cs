//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetPolicyCodeQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;

namespace eRaven.Application.Handlers.Timesheet;

public sealed class GetTimesheetPolicyForCodeOptionQueryHandler(
    ITimesheetPolicyRepository repo)
    : IQueryHandler<GetTimesheetPolicyForCodeQuery, IReadOnlyList<TimesheetTransitionOptionDto>>
{
    private readonly ITimesheetPolicyRepository _repo = repo;

    public async Task<IReadOnlyList<TimesheetTransitionOptionDto>> HandleAsync(
        GetTimesheetPolicyForCodeQuery query,
        CancellationToken ct = default)
    {
        var policies = await _repo.GetAllowedTransitionsAsync(query.CodeId, ct);

        return [.. policies
           .Where(x => x.ToCode.IsActive)
           .Where(x => !x.ToCode.Code.Equals(TimesheetSystemCodes.NotInTimesheet))
           .OrderBy(x => x.ToCode.SortOrder)
           .ThenBy(x => x.ToCode.Priority)
           .ThenBy(x => x.ToCode.Code)
           .Select(x => new TimesheetTransitionOptionDto(
               x.ToCode.Code,
               x.ToCode.Title,
               x.StartShiftDays))];
    }
}
