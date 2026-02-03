//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetMissionParticipationMonthQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

/// <summary>
/// Повертає список участей, що перетинають місяць (overlap).
/// </summary>
public sealed class GetMissionParticipationMonthQueryHandler(
    IMissionParticipationRepository repo)
    : IQueryHandler<GetMissionParticipationMonthQuery, IReadOnlyList<MissionParticipationRowDto>>
{
    private readonly IMissionParticipationRepository _repo = repo;

    public async Task<IReadOnlyList<MissionParticipationRowDto>> HandleAsync(
        GetMissionParticipationMonthQuery query,
        CancellationToken ct = default)
        => await _repo.GetOverlappingMonthAsync(query.Year, query.Month, query.MissionId, query.Search, ct);
}