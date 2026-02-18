//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeCallsingCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonInfo;

namespace eRaven.Application.Handlers.Personal;

public class ChangeCallsignCommandHandler(IPersonRepository repo)
    : ICommandHandler<ChangeCallsignCommand>
{
    private readonly IPersonRepository _repo = repo;

    public async Task HandleAsync(ChangeCallsignCommand command, CancellationToken ct = default)
        => await _repo.ChangeCallsignAsync(command.PersonId,
            command.EffectiveDate,
            command.Callsign,
            command.Author,
            command.NowUtc,
            ct: ct);
}
