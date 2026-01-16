//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateReservedCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonMove;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers.Personal;

public sealed class CreateReservedCommandHandler(IPersonRepository repo)
    : ICommandHandler<CreateReservedCommand, Guid>
{
    private readonly IPersonRepository _repo = repo;

    public async Task<Guid> HandleAsync(CreateReservedCommand command, CancellationToken ct = default)
    {
        return await _repo.CreateReservedAsync(cmd: command, ct: ct);
    }
}
