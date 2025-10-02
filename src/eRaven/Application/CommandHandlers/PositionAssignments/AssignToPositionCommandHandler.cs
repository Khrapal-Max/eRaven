//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AssignToPositionCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PositionAssignments;
using eRaven.Application.Repositories;
using eRaven.Domain.Services;

namespace eRaven.Application.CommandHandlers.PositionAssignments;

public sealed class AssignToPositionCommandHandler(
    IPersonRepository personRepository,
    IPositionAssignmentPolicy policy)
        : ICommandHandler<AssignToPositionCommand>
{
    private readonly IPersonRepository _personRepository = personRepository;
    private readonly IPositionAssignmentPolicy _policy = policy;

    public async Task HandleAsync(
        AssignToPositionCommand cmd,
        CancellationToken ct = default)
    {
        var person = await _personRepository.GetByIdAsync(cmd.PersonId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена");

        person.AssignToPosition(
            cmd.PositionUnitId,
            cmd.OpenUtc,
            _policy,
            cmd.Note
        );

        await _personRepository.UpdateAsync(person, ct);
    }
}
