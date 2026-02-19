//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetMissionsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.MissionRepository;
using eRaven.Application.DTOs.Missions;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Missions;

namespace eRaven.Application.Handlers.Missions;

public sealed class GetMissionsQueryHandler(IMissionRepository repo)
    : IQueryHandler<GetMissionsQuery, IReadOnlyList<MissionDto>>
{
    private readonly IMissionRepository _repo = repo;

    public async Task<IReadOnlyList<MissionDto>> HandleAsync(GetMissionsQuery query, CancellationToken ct = default)
    {
        var all = await _repo.GetMissionsAsync(ct);

        var q = all.AsEnumerable();

        if (query.OnlyOpen)
            q = q.Where(x => x.ClosedAt is null);

        if (query.Mode is not null)
            q = q.Where(x => x.MissionMode == query.Mode);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(x =>
                x.PositionArea.Contains(s, StringComparison.OrdinalIgnoreCase)
                || (x.NamePoint != null && x.NamePoint.Contains(s, StringComparison.OrdinalIgnoreCase))
                || x.Target.Contains(s, StringComparison.OrdinalIgnoreCase)
                || (x.TypeDrone != null && x.TypeDrone.Contains(s, StringComparison.OrdinalIgnoreCase)));
        }

        return [.. q
            .OrderBy(x => x.ClosedAt is null ? 0 : 1) // open first
            .ThenByDescending(x => x.CreatedAt)
            .ThenBy(x => x.PositionArea)
            .Select(x => new MissionDto(
                MissionId: x.Id,
                PositionArea: x.PositionArea,
                NamePoint: x.NamePoint,
                Target: x.Target,
                MissionMode: x.MissionMode,
                DroneName: x.TypeDrone,
                DisplayMisssion: x.ToString(),
                CreatedAt: x.CreatedAt,
                ClosedAt: x.ClosedAt,
                IsOpen: x.ClosedAt is null
            ))];
    }
}
