//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetDayQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.Timesheets;
using eRaven.Application.Mapper;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;

namespace eRaven.Application.Handlers.Timesheets;

/// <summary>
/// Query handler: повертає денний зріз табеля.
///
/// <para>Контракт DTO уніфіковано:</para>
/// повертаємо <see cref="TimesheetPersonRangeRowDto"/> з <c>Days.Count == 1</c>.
/// </summary>
public sealed class GetTimesheetDayQueryHandler(
    ITimesheetViewRepository repo,
    IPersonRepository persons)
    : IQueryHandler<GetTimesheetDayQuery, IReadOnlyList<TimesheetPersonRangeRowDto>>
{
    private readonly ITimesheetViewRepository _repo = repo;
    private readonly IPersonRepository _persons = persons;

    public async Task<IReadOnlyList<TimesheetPersonRangeRowDto>> HandleAsync(
        GetTimesheetDayQuery query,
        CancellationToken ct = default)
    {
        var periods = await _repo.GetTimesheetsDayAsync(query.Date, ct);
        if (periods.Count == 0) return [];

        var personIds = periods.Select(x => x.PersonId).Distinct().ToArray();
        var cards = await _persons.GetByIdsAsync(personIds, ct);

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

        var byId = cards
            .Where(p => search is null || TimesheetDtoMapper.MatchesSearch(p, search))
            .ToDictionary(p => p.Id, p => p);

        var periodByPersonId = periods.ToDictionary(x => x.PersonId, x => x);

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
