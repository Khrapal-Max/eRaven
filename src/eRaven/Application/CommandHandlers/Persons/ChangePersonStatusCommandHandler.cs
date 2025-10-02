//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangePersonStatusCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.Persons;
using eRaven.Application.Repositories;
using eRaven.Domain.Services;

namespace eRaven.Application.CommandHandlers.Persons;

public sealed class ChangePersonStatusCommandHandler(
    IPersonRepository personRepository,
    IStatusTransitionValidator transitionValidator)
        : ICommandHandler<ChangePersonStatusCommand>
{
    private readonly IPersonRepository _personRepository = personRepository;
    private readonly IStatusTransitionValidator _transitionValidator = transitionValidator;

    public async Task HandleAsync(
        ChangePersonStatusCommand cmd,
        CancellationToken ct = default)
    {
        var person = await _personRepository.GetByIdAsync(cmd.PersonId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена");

        person.ChangeStatus(
            cmd.NewStatusKindId,
            cmd.EffectiveAtUtc,
            _transitionValidator,
            cmd.Note,
            cmd.Author
        );

        await _personRepository.UpdateAsync(person, ct);
    }
}
