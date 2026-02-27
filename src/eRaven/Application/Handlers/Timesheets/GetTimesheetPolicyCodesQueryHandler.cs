//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetPolicyCodesQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.DTOs.Timesheets.Policy;
using eRaven.Application.Mapper;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;

namespace eRaven.Application.Handlers.Timesheets;

/// <summary>
/// Хендлер повернення кодів з довідника.
/// </summary>
public sealed class GetTimesheetPolicyCodesQueryHandler(
    ITimesheetPolicyRepository repo)
    : IQueryHandler<GetTimesheetPolicyCodesQuery, IReadOnlyList<TimesheetCodeDto>>
{
    private readonly ITimesheetPolicyRepository _repo = repo;

    ///  <inheritdoc/>
    public async Task<IReadOnlyList<TimesheetCodeDto>> HandleAsync(
        GetTimesheetPolicyCodesQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

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
                c.IsActive,
                TimesheetEnumMapper.MapRole(c.RoleCode),
                TimesheetEnumMapper.MapStyle(c.UiStyle)))];
    }
}