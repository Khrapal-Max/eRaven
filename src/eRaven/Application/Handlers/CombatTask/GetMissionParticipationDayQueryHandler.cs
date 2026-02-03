//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetMissionParticipationDayQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

/// <summary>
/// Повертає список участей на дату (overlap).
/// </summary>
public sealed class GetMissionParticipationDayQueryHandler(
    IMissionParticipationRepository repo)
    : IQueryHandler<GetMissionParticipationDayQuery, IReadOnlyList<MissionParticipationRowDto>>
{
    private readonly IMissionParticipationRepository _repo = repo;

    public async Task<IReadOnlyList<MissionParticipationRowDto>> HandleAsync(
        GetMissionParticipationDayQuery query,
        CancellationToken ct = default)
        => await _repo.GetOnDateAsync(query.Date, query.MissionId, query.Search, ct);
}