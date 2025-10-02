//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePlanActionCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PlanActions;
using eRaven.Application.Repositories;

namespace eRaven.Application.CommandHandlers.PlanActions;

public sealed class CreatePlanActionCommandHandler(IPersonRepository personRepository)
        : ICommandHandler<CreatePlanActionCommand, Guid>
{
    private readonly IPersonRepository _personRepository = personRepository;

    public async Task<Guid> HandleAsync(
        CreatePlanActionCommand cmd,
        CancellationToken ct = default)
    {
        var person = await _personRepository.GetByIdAsync(cmd.PersonId, ct)
             ?? throw new InvalidOperationException("Особа не знайдена");

        person.CreatePlanAction(
            cmd.PlanActionName,
            cmd.EffectiveAtUtc,
            cmd.MoveType,
            cmd.Location,
            cmd.GroupName,
            cmd.CrewName,
            cmd.Note
        );

        await _personRepository.UpdateAsync(person, ct);

        // Повертаємо Id створеної дії
        var action = person.PlanActions
            .OrderByDescending(a => a.EffectiveAtUtc)
            .First();

        return action.Id;
    }
}
