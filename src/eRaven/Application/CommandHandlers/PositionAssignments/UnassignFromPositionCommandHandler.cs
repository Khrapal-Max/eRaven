//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// UnassignFromPositionCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PositionAssignments;
using eRaven.Application.Repositories;

namespace eRaven.Application.CommandHandlers.PositionAssignments;

public sealed class UnassignFromPositionCommandHandler(IPersonRepository personRepository)
        : ICommandHandler<UnassignFromPositionCommand>
{
    private readonly IPersonRepository _personRepository = personRepository;

    public async Task HandleAsync(
        UnassignFromPositionCommand cmd,
        CancellationToken ct = default)
    {
        var person = await _personRepository.GetByIdAsync(cmd.PersonId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена");

        person.UnassignFromPosition(cmd.CloseUtc, cmd.Note);

        await _personRepository.UpdateAsync(person, ct);
    }
}