//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeRankCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonInfo;

namespace eRaven.Application.Handlers.Personal;

public sealed class ChangeRankCommandHandler(IPersonRepository repo)
    : ICommandHandler<ChangeRankCommand>
{
    private readonly IPersonRepository _repo = repo;

    public async Task HandleAsync(ChangeRankCommand command, CancellationToken ct = default)
        => await _repo.ChangeRankAsync(command.PersonId,
            command.EffectiveDate,
            command.Rank,
            command.Note,
            command.Author,
            command.NowUtc,
            ct: ct);
}
