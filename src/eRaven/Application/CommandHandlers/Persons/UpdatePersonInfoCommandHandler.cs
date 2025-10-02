//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// UpdatePersonInfoCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.Persons;
using eRaven.Application.Repositories;
using eRaven.Domain.ValueObjects;

namespace eRaven.Application.CommandHandlers.Persons;

public sealed class UpdatePersonInfoCommandHandler(IPersonRepository personRepository)
        : ICommandHandler<UpdatePersonInfoCommand>
{
    private readonly IPersonRepository _personRepository = personRepository;

    public async Task HandleAsync(
        UpdatePersonInfoCommand cmd,
        CancellationToken ct = default)
    {
        var person = await _personRepository.GetByIdAsync(cmd.PersonId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена");

        var newPersonalInfo = new PersonalInfo(
            person.PersonalInfo.Rnokpp, // РНОКПП не міняємо
            cmd.LastName,
            cmd.FirstName,
            cmd.MiddleName
        );

        var newMilitaryDetails = new MilitaryDetails(
            cmd.Rank,
            cmd.BZVP,
            cmd.Weapon,
            cmd.Callsign
        );

        person.UpdatePersonalInfo(newPersonalInfo);
        person.UpdateMilitaryDetails(newMilitaryDetails);

        await _personRepository.UpdateAsync(person, ct);
    }
}