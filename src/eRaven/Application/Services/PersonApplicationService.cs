//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonApplicationService
//-----------------------------------------------------------------------------

using eRaven.Application.Repositories;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Enums;
using eRaven.Domain.Services;
using eRaven.Domain.ValueObjects;

namespace eRaven.Application.Services;

public class PersonApplicationService(
    IPersonRepository personRepository,
    IStatusTransitionValidator statusTransitionValidator,
    IPositionAssignmentPolicy positionAssignmentPolicy)
{
    private readonly IPersonRepository _personRepository = personRepository;
    private readonly IStatusTransitionValidator _statusTransitionValidator = statusTransitionValidator;
    private readonly IPositionAssignmentPolicy _positionAssignmentPolicy = positionAssignmentPolicy;

    // ==================== Створення ====================

    public async Task<Guid> CreatePersonAsync(
        string rnokpp, string lastName, string firstName, string? middleName,
        string rank, string bzvp, string? weapon, string? callsign,
        int initialStatusKindId,
        CancellationToken ct = default)
    {
        // 1. Перевірка унікальності РНОКПП
        var existing = await _personRepository.GetByRnokppAsync(rnokpp, ct);
        if (existing != null)
            throw new InvalidOperationException("Особа з таким РНОКПП вже існує");

        // 2. Створюємо Value Objects
        var personalInfo = new PersonalInfo(rnokpp, lastName, firstName, middleName);
        var militaryDetails = new MilitaryDetails(rank, bzvp, weapon, callsign);

        // 3. Створюємо агрегат
        var person = PersonAggregate.Create(
            personalInfo,
            militaryDetails,
            initialStatusKindId,
            _statusTransitionValidator);

        // 4. Зберігаємо
        await _personRepository.AddAsync(person, ct);

        return person.Id;
    }

    // ==================== Зміна статусу ====================

    public async Task ChangePersonStatusAsync(
        Guid personId,
        int newStatusKindId,
        DateTime effectiveAtUtc,
        string? note = null,
        string? author = null,
        CancellationToken ct = default)
    {
        // 1. Завантажуємо агрегат
        var person = await _personRepository.GetByIdAsync(personId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена");

        // 2. Викликаємо бізнес-логіку агрегату
        person.ChangeStatus(
            newStatusKindId,
            effectiveAtUtc,
            _statusTransitionValidator,
            note,
            author);

        // 3. Зберігаємо
        await _personRepository.UpdateAsync(person, ct);
    }

    // ==================== Призначення на посаду ====================

    public async Task AssignToPositionAsync(
        Guid personId,
        Guid positionUnitId,
        DateTime openUtc,
        string? note = null,
        CancellationToken ct = default)
    {
        var person = await _personRepository.GetByIdAsync(personId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена");

        person.AssignToPosition(
            positionUnitId,
            openUtc,
            _positionAssignmentPolicy,
            note);

        await _personRepository.UpdateAsync(person, ct);
    }

    public async Task UnassignFromPositionAsync(
        Guid personId,
        DateTime closeUtc,
        string? note = null,
        CancellationToken ct = default)
    {
        var person = await _personRepository.GetByIdAsync(personId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена");

        person.UnassignFromPosition(closeUtc, note);

        await _personRepository.UpdateAsync(person, ct);
    }

    // ==================== Планові дії ====================

    public async Task CreatePlanActionAsync(
        Guid personId,
        string planActionName,
        DateTime effectiveAtUtc,
        MoveType moveType,
        string location,
        string? groupName = null,
        string? crewName = null,
        string? note = null,
        CancellationToken ct = default)
    {
        var person = await _personRepository.GetByIdAsync(personId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена");

        person.CreatePlanAction(
            planActionName,
            effectiveAtUtc,
            moveType,
            location,
            groupName,
            crewName,
            note);

        await _personRepository.UpdateAsync(person, ct);
    }
}