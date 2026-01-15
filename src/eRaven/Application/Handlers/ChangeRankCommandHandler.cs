//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeRankCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonInfo;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers;

public sealed class ChangeRankCommandHandler(IPersonRepository repo)
    : ICommandHandler<ChangeRankCommand, Guid>
{
    private readonly IPersonRepository _repo = repo;

    public async Task<Guid> HandleAsync(ChangeRankCommand command, CancellationToken ct = default)
    {
        await _repo.ChangeRankAsync(cmd: command, ct: ct);
        return command.PersonId;
    }
}
