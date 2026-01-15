//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcludeCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonMove;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers;

public sealed class ExcludeCommandHandler(IPersonRepository repo)
    : ICommandHandler<ExcludeCommand>
{
    private readonly IPersonRepository _repo = repo;

    public async Task HandleAsync(ExcludeCommand command, CancellationToken ct = default)
        => await _repo.ExcludeAsync(command, ct);
}
