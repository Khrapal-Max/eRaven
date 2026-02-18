//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// VoidPersonEventCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonMove;

namespace eRaven.Application.Handlers.Personal;

public class VoidPersonEventCommandHandler(IPersonRepository repo)
    : ICommandHandler<VoidPersonEventCommand>
{
    private readonly IPersonRepository _repo = repo;

    public async Task HandleAsync(VoidPersonEventCommand command, CancellationToken ct = default)
        => await _repo.VoidEventAsync(command.PersonId,
            command.TargetEventId,
            command.Reason,
            command.Author,
            command.NowUtc,
            ct: ct);
}