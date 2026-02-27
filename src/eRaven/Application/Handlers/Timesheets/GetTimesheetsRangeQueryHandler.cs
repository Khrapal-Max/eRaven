//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetRangeQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.Timesheets;
using eRaven.Application.Mapper;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;

namespace eRaven.Application.Handlers.Timesheets;

/// <summary>
/// Query handler: повертає табельну матрицю по діапазону дат (inclusive) для UI/операцій.
/// </summary>
public sealed class GetTimesheetsRangeQueryHandler(
    ITimesheetViewRepository repo,
    IPersonRepository persons)
    : IQueryHandler<GetTimesheetsRangeQuery, IReadOnlyList<TimesheetPersonRangeRowDto>>
{
    private readonly ITimesheetViewRepository _repo = repo;
    private readonly IPersonRepository _persons = persons;

    public async Task<IReadOnlyList<TimesheetPersonRangeRowDto>> HandleAsync(
        GetTimesheetsRangeQuery query,
        CancellationToken ct = default)
    {
        var periods = await _repo.GetTimesheetsRangeAsync(query.From, query.To, ct);
        if (periods.Count == 0) return [];

        var personIds = periods.Select(x => x.PersonId).Distinct().ToArray();
        var cards = await _persons.GetByIdsAsync(personIds, ct);

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

        var byId = cards
            .Where(p => search is null || TimesheetDtoMapper.MatchesSearch(p, search))
            .ToDictionary(p => p.Id, p => p);

        var periodByPersonId = periods
            .GroupBy(x => x.PersonId)
            .ToDictionary(g => g.Key, g => g.First());

        var rows = new List<TimesheetPersonRangeRowDto>(byId.Count);

        foreach (var p in byId.Values
                     .OrderBy(x => x.PositionSort ?? int.MaxValue)
                     .ThenBy(x => x.FullName))
        {
            if (!periodByPersonId.TryGetValue(p.Id, out var period))
                continue;

            var personDto = TimesheetDtoMapper.MapPerson(p);
            var dayDtos = period.Days.Select(TimesheetDtoMapper.MapDay).ToList();

            rows.Add(new TimesheetPersonRangeRowDto(personDto, dayDtos));
        }

        return rows;
    }
}
