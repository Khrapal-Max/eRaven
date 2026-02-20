//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetPolicyForCodeQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.DTOs.Timesheets.Policy;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;

namespace eRaven.Application.Handlers.Timesheets;

public sealed class GetTimesheetPolicyForCodeQueryHandler(
    ITimesheetPolicyRepository repo)
    : IQueryHandler<GetTimesheetPolicyForCodeQuery, TimesheetPolicyEditorDto?>
{
    private readonly ITimesheetPolicyRepository _repo = repo;

    public async Task<TimesheetPolicyEditorDto?> HandleAsync(
        GetTimesheetPolicyForCodeQuery query,
        CancellationToken ct = default)
    {
        if (query.CodeId == Guid.Empty)
            throw new ArgumentException("CodeId is required.", nameof(query.CodeId));

        var code = await _repo.GetCodeByIdAsync(query.CodeId, ct);
        if (code is null)
            return null;

        var transitions = await _repo.GetAllowedTransitionsAsync(query.CodeId, ct);

        return new TimesheetPolicyEditorDto(
            Code: new TimesheetCodeDto(
                code.Id,
                code.Code,
                code.Title,
                code.Description,
                code.SortOrder,
                code.Priority,
                code.IsTerminal,
                code.IsActive),
            AllowedTransitions: [.. transitions
            .Select(t => new TimesheetTransitionSpecDto(t.ToCodeId, t.StartShiftDays))]
        );
    }
}