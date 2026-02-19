//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetDayQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.Timesheets;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;

namespace eRaven.Application.Handlers.Timesheets;

/// <summary>
/// Query handler: денний зріз табеля (стан на дату) для UI/дашбордів.
/// </summary>
public sealed class GetTimesheetDayQueryHandler(ITimesheetViewRepository repo)
    : IQueryHandler<GetTimesheetDayQuery, IReadOnlyList<TimesheetPersonDayRowDto>>
{
    private readonly ITimesheetViewRepository _repo = repo;

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetPersonDayRowDto>> HandleAsync(
        GetTimesheetDayQuery query,
        CancellationToken ct = default)
    {
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        return await _repo.GetTimesheetDayAsync(query.Date, search, ct);
    }
}
