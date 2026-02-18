//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CloseMissionCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.MissionRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Mission;

namespace eRaven.Application.Handlers.Mission;

public sealed class CloseMissionCommandHandler(IMissionRepository repo)
    : ICommandHandler<CloseMissionCommand>
{
    private readonly IMissionRepository _repo = repo;

    public async Task HandleAsync(CloseMissionCommand command, CancellationToken ct = default)
    {
        await _repo.CloseMissionAsync(command.MissionId, command.ClosedAt, ct);
    }
}
