//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonMove;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers;

public sealed class EnrollCommandHandler(IPersonRepository repo)
    : ICommandHandler<EnrollCommand, Guid>
{
    private readonly IPersonRepository _repo = repo;

    public async Task<Guid> HandleAsync(EnrollCommand command, CancellationToken ct = default)
        => await _repo.EnrollAsync(command, ct);
}