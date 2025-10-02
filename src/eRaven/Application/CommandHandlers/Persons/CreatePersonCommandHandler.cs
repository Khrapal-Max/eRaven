//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePersonCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.Persons;
using eRaven.Application.Repositories;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Services;
using eRaven.Domain.ValueObjects;

namespace eRaven.Application.CommandHandlers.Persons;

public sealed class CreatePersonCommandHandler(
    IPersonRepository personRepository,
    IStatusTransitionValidator transitionValidator)
        : ICommandHandler<CreatePersonCommand, Guid>
{
    private readonly IPersonRepository _personRepository = personRepository;
    private readonly IStatusTransitionValidator _transitionValidator = transitionValidator;

    public async Task<Guid> HandleAsync(
        CreatePersonCommand cmd,
        CancellationToken ct = default)
    {
        // 1. Перевірка унікальності РНОКПП
        var existing = await _personRepository.GetByRnokppAsync(cmd.Rnokpp, ct);
        if (existing != null)
            throw new InvalidOperationException("Особа з таким РНОКПП вже існує");

        // 2. Створення Value Objects
        var personalInfo = new PersonalInfo(
            cmd.Rnokpp,
            cmd.LastName,
            cmd.FirstName,
            cmd.MiddleName
        );

        var militaryDetails = new MilitaryDetails(
            cmd.Rank,
            cmd.BZVP,
            cmd.Weapon,
            cmd.Callsign
        );

        // 3. Створення агрегату (початковий статус "Рекрут" = 1)
        var person = PersonAggregate.Create(
            personalInfo,
            militaryDetails,
            initialStatusKindId: 1, // "Рекрут"
            _transitionValidator
        );

        // 4. Збереження
        await _personRepository.AddAsync(person, ct);

        return person.Id;
    }
}