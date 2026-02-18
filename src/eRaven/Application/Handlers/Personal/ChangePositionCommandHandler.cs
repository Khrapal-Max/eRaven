//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonCard
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonInfo;

namespace eRaven.Application.Handlers.Personal;

public sealed class ChangePositionCommandHandler(IPersonRepository repo)
    : ICommandHandler<ChangePositionCommand>
{
    private readonly IPersonRepository _repo = repo;

    public async Task HandleAsync(ChangePositionCommand command, CancellationToken ct = default)
        => await _repo.ChangePositionAsync(command.PersonId,
            command.EffectiveDate,
            command.PositionSort,
            command.Position,
            command.Note,
            command.Author,
            command.NowUtc,
            ct: ct);
}
