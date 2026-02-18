//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetRangeQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;

namespace eRaven.Application.Handlers.Timesheet;

/// <summary>
/// Query handler: повертає табельну матрицю по діапазону дат (inclusive) для UI/операцій.
/// </summary>
public sealed class GetTimesheetRangeQueryHandler(ITimesheetViewRepository repo)
    : IQueryHandler<GetTimesheetRangeQuery, IReadOnlyList<TimesheetPersonRangeRowDto>>
{
    private readonly ITimesheetViewRepository _repo = repo;

    /// <summary>
    /// Повертає дані по табелю для всіх осіб за вказаний період.
    /// Якщо вказано рядок пошуку, повертає лише тих осіб, у яких ПІБ або РНОКПП містять цей рядок.
    /// </summary>
    public async Task<IReadOnlyList<TimesheetPersonRangeRowDto>> HandleAsync(
        GetTimesheetRangeQuery query,
        CancellationToken ct = default)
    {
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        return await _repo.GetTimesheetRangeAsync(query.From, query.To, search, ct);
    }
}
