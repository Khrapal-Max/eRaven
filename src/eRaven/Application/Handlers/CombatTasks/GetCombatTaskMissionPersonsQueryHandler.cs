//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskMissionPersonsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.CombatTasks;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTasks;

namespace eRaven.Application.Handlers.CombatTasks;

/// <summary>
/// Query handler: повертає людей, які активні по місії на дату.
/// Джерело правди — CombatTask (MissionAssignments).
/// </summary>
public sealed class GetCombatTaskMissionPersonsQueryHandler(
    ITimesheetMissionPlanningRepository repo,
    IPersonRepository persons)
    : IQueryHandler<GetCombatTaskMissionPersonsQuery, IReadOnlyList<ActiveMissionPersonDto>>
{
    private readonly ITimesheetMissionPlanningRepository _repo = repo;
    private readonly IPersonRepository _persons = persons;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ActiveMissionPersonDto>> HandleAsync(
        GetCombatTaskMissionPersonsQuery query,
        CancellationToken ct = default)
    {
        var personIds = await _repo.GetActiveMissionPersonsAsync(query.MissionId, query.OnDate, ct);
        if (personIds.Count == 0)
            return [];

        var people = await _persons.GetByIdsAsync(personIds, ct);
        if (people.Count == 0)
            return [];

        // Мінімальна дата початку активного призначення по місії для кожної особи.
        var fromByPerson = await _repo.GetActiveMissionPersonFromDatesAsync(
            query.MissionId,
            query.OnDate,
            personIds,
            ct);

        return [.. people
            .OrderBy(x => x.FullName)
            .ThenBy(x => x.Id)
            .Select(p => new ActiveMissionPersonDto(
                PersonId: p.Id,
                Rnokpp: p.Rnokpp,
                FullName: p.FullName,
                Callsign: p.Callsign,
                Rank: p.Rank,
                Position: p.Position,
                Weapon: p.Weapon,
                From: fromByPerson.TryGetValue(p.Id, out var from) ? from : query.OnDate))];
    }
}
