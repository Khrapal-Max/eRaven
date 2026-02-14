//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// UpdatePersonalInfoCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonInfo;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers.Personal;

public class UpdatePersonalInfoCommandHandler(IPersonRepository repo)
    : ICommandHandler<UpdatePersonalInfoCommand>
{
    private readonly IPersonRepository _repo = repo;

    public async Task HandleAsync(UpdatePersonalInfoCommand command, CancellationToken ct = default)
        => await _repo.UpdatePersonalInfoAsync(command.PersonId,
            command.Rnokpp,
            command.LastName,
            command.FirstName,
            command.MiddleName,
            command.Note,
            command.Author,
            command.NowUtc,
            ct: ct);
}
