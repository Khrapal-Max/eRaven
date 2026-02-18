//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetMonthQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;

namespace eRaven.Application.Handlers.Timesheet;

/// <summary>
/// Query handler: повертає місячну матрицю табеля (grid) для UI.
/// </summary>
public sealed class GetTimesheetMonthQueryHandler(ITimesheetViewRepository repo)
    : IQueryHandler<GetTimesheetMonthQuery, IReadOnlyList<TimesheetPersonMonthRowDto>>
{
    private readonly ITimesheetViewRepository _repo = repo;

    /// <inheritdoc />
    public Task<IReadOnlyList<TimesheetPersonMonthRowDto>> HandleAsync(
        GetTimesheetMonthQuery query,
        CancellationToken ct = default)
        => _repo.GetTimesheetMonthAsync(query.Year, query.Month, query.Search, ct);
}
