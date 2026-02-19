//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskPersonLookupQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.CombatTasks;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTasks;

namespace eRaven.Application.Handlers.CombatTasks;

/// <summary>
/// Query handler: повертає людей, доступних для призначення на завдання.
/// </summary>
public sealed class GetCombatTaskPersonLookupQueryHandler(
    ITimesheetMissionPlanningRepository repo)
    : IQueryHandler<GetCombatTaskPersonLookupQuery, IReadOnlyList<ReadyCombatTaskPersonDto>>
{
    private readonly ITimesheetMissionPlanningRepository _repo = repo;

    public async Task<IReadOnlyList<ReadyCombatTaskPersonDto>> HandleAsync(
        GetCombatTaskPersonLookupQuery query,
        CancellationToken ct = default)
        => await _repo.GetFreePersonForMissionsAsync(query.OnDate, ct);
}
