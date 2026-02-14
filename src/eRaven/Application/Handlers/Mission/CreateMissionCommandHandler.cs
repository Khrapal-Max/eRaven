//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateMissionCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.Mission;
using eRaven.Infrastructure.Repositories.MissionRepository;

namespace eRaven.Application.Handlers.Mission;

public sealed class CreateMissionCommandHandler(IMissionRepository repo)
    : ICommandHandler<CreateMissionCommand, Guid>
{
    private readonly IMissionRepository _repo = repo;

    public async Task<Guid> HandleAsync(CreateMissionCommand command, CancellationToken ct = default)
    {
        return await _repo.AddMission(
            positionArea: command.PositionArea,
            namePoint: command.NamePoint,
            typeDrone: command.DroneName,
            target: command.Target,
            missionMode: command.MissionMode,
            todayLocal: command.TodayLocal,
            ct: ct);
    }
}