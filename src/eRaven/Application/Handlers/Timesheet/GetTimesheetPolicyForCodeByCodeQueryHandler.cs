//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetPolicyCodeQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;

namespace eRaven.Application.Handlers.Timesheet;

public sealed class GetTimesheetPolicyForCodeByCodeQueryHandler(
    ITimesheetPolicyRepository repo)
    : IQueryHandler<GetTimesheetPolicyForCodeByCodeQuery, IReadOnlyList<TimesheetTransitionOptionDto>>
{
    private readonly ITimesheetPolicyRepository _repo = repo;

    public async Task<IReadOnlyList<TimesheetTransitionOptionDto>> HandleAsync(
        GetTimesheetPolicyForCodeByCodeQuery query,
        CancellationToken ct = default)
        => await _repo.GetAllowedTransitionOptionsAsync(query.Code, ct);
}
