//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetRangeQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Infrastructure.Repositories.TimesheetRepository;

namespace eRaven.Application.Handlers.Timesheet;

public sealed class GetTimesheetRangeQueryHandler(
    ITimesheetMonthRepository repo)
    : IQueryHandler<GetTimesheetRangeQuery, IReadOnlyList<TimesheetPersonRangeRowDto>>
{
    private readonly ITimesheetMonthRepository _repo = repo;

    /// <summary>
    /// Повертає дані по відвідуваності для всіх працівників за вказаний період.
    /// Якщо вказано рядок пошуку, то повертає лише тих працівників, у яких ПІБ або РНОКПП містить цей рядок.
    /// </summary>
    public async Task<IReadOnlyList<TimesheetPersonRangeRowDto>> HandleAsync(
        GetTimesheetRangeQuery query,
        CancellationToken ct = default)
    {
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        return await _repo.GetTimesheetRangeAsync(query.From, query.To, search, ct);
    }
}
