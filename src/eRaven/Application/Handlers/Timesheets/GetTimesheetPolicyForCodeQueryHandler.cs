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

/// <summary>
/// Хендлер повернення політики кодів закріплених за певним кодом.
/// </summary>
public sealed class GetTimesheetPolicyForCodeQueryHandler(
    ITimesheetPolicyRepository repo)
    : IQueryHandler<GetTimesheetPolicyForCodeQuery, TimesheetPolicyEditorDto?>
{
    private readonly ITimesheetPolicyRepository _repo = repo;

    ///  <inheritdoc/>
    public async Task<TimesheetPolicyEditorDto?> HandleAsync(
        GetTimesheetPolicyForCodeQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CodeId == Guid.Empty)
            throw new InvalidOperationException("CodeId обов'язковий.");

        var code = await _repo.GetCodeByIdAsync(query.CodeId, ct);
        if (code is null)
            return null;

        var transitions = await _repo.GetAllowedCodesAsync(query.CodeId, ct);

        return new TimesheetPolicyEditorDto(
            Code: new TimesheetCodeDto(
                code.Id,
                code.Code,
                code.Title,
                code.Description,
                code.SortOrder,
                code.Priority,
                code.IsTerminal,
                code.IsActive,
                code.RoleCode,
                code.UiStyle),
            AllowedTransitions: [.. transitions
            .Select(t => new TimesheetTransitionSpecDto(t.ToCodeId, t.StartShiftDays))]
        );
    }
}