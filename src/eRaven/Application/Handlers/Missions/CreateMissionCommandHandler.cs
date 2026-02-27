//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateMissionCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.MissionRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Missions;
using eRaven.Application.Mapper;

namespace eRaven.Application.Handlers.Missions;

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
            missionMode: PersonMissionEnumDtoMapper.ToDomain(command.MissionMode),
            todayLocal: command.TodayLocal,
            ct: ct);
    }
}
