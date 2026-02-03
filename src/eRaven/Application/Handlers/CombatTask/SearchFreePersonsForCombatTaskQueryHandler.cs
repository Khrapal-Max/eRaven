//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SearchFreePersonsForCombatTaskQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers.CombatTask;

/// <summary>
/// Повертає осіб для picker-а, які вільні на дату (не мають overlap у MissionParticipation).
/// </summary>
public sealed class SearchFreePersonsForCombatTaskQueryHandler(
    IPersonRepository persons,
    IMissionParticipationRepository missionParticipation)
    : IQueryHandler<SearchFreePersonsForCombatTaskQuery, IReadOnlyList<CombatTaskPersonLookupDto>>
{
    private readonly IPersonRepository _persons = persons;
    private readonly IMissionParticipationRepository _missionParticipation = missionParticipation;

    public async Task<IReadOnlyList<CombatTaskPersonLookupDto>> HandleAsync(
        SearchFreePersonsForCombatTaskQuery query,
        CancellationToken ct = default)
    {
        var search = string.IsNullOrWhiteSpace(query.Search) ? string.Empty : query.Search.Trim();
        var take = query.Take <= 0 ? 50 : query.Take;

        // 1) кандидатів беремо з person repo
        var candidates = await _persons.GetPersonsSearchAsync(search, take, ct);
        if (candidates.Count == 0)
            return [];

        // 2) зайняті на дату (через mission repo)
        var busyIds = await _missionParticipation.GetBusyPersonIdsOnDateAsync(query.Date, ct);
        if (busyIds.Count == 0)
            return candidates;

        var busy = busyIds.ToHashSet();
        return [.. candidates.Where(p => !busy.Contains(p.PersonId))];
    }
}