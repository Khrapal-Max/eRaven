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
        => await _repo.CreateReservedAsync(command.Rnokpp,
                command.LastName,
                command.FirstName,
                command.MiddleName,
                command.Rank,
                command.Position,
                command.Author,
                command.NowUtc,
                ct: ct);
}
