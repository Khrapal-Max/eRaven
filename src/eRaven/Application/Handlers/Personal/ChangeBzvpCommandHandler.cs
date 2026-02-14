//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeBZVPCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonInfo;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers.Personal;

public class ChangeBzvpCommandHandler(IPersonRepository repo)
    : ICommandHandler<ChangeBzvpCommand>
{
    private readonly IPersonRepository _repo = repo;

    public async Task HandleAsync(ChangeBzvpCommand command, CancellationToken ct = default)
        => await _repo.ChangeBzvpAsync(command.PersonId,
            command.EffectiveDate,
            command.Bzvp,
            command.Note,
            command.Author,
            command.NowUtc,
            ct: ct);
}
