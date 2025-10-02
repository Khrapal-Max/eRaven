//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ApprovePlanActionCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PlanActions;
using eRaven.Application.Repositories;

namespace eRaven.Application.CommandHandlers.PlanActions;

public sealed class ApprovePlanActionCommandHandler(IPersonRepository personRepository)
        : ICommandHandler<ApprovePlanActionCommand>
{
    private readonly IPersonRepository _personRepository = personRepository;

    public async Task HandleAsync(
        ApprovePlanActionCommand cmd,
        CancellationToken ct = default)
    {
        var person = await _personRepository.GetByIdAsync(cmd.PersonId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена");

        person.ApprovePlanAction(cmd.ActionId, cmd.Order);

        await _personRepository.UpdateAsync(person, ct);
    }
}