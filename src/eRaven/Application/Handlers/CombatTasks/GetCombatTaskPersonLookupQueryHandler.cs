//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskPersonLookupQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.CombatTasks;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTasks;

namespace eRaven.Application.Handlers.CombatTasks;

/// <summary>
/// Query handler: повертає людей, доступних для призначення на завдання.
/// </summary>
public sealed class GetCombatTaskPersonLookupQueryHandler(
    ITimesheetMissionPlanningRepository repo,
    IPersonRepository persons)
    : IQueryHandler<GetCombatTaskPersonLookupQuery, IReadOnlyList<ReadyCombatTaskPersonDto>>
{
    private readonly ITimesheetMissionPlanningRepository _repo = repo;
    private readonly IPersonRepository _persons = persons;

    public async Task<IReadOnlyList<ReadyCombatTaskPersonDto>> HandleAsync(
        GetCombatTaskPersonLookupQuery query,
        CancellationToken ct = default)
    {
        if (query.OnDate == default)
            throw new InvalidOperationException("OnDate обов'язковий.");

        var ids = await _repo.GetFreePersonForMissionsAsync(query.OnDate, ct);
        if (ids.Count == 0)
            return [];

        var people = await _persons.GetByIdsAsync(ids, ct);

        return [.. people
            .OrderBy(x => x.FullName)
            .ThenBy(x => x.Id)
            .Select(p => new ReadyCombatTaskPersonDto(
                PersonId: p.Id,
                Rnokpp: p.Rnokpp,
                FullName: p.FullName,
                Rank: p.Rank,
                Position: p.Position,
                Weapon: p.Weapon,
                Callsign: p.Callsign))];
    }
}
