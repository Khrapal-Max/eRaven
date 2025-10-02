//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Person (Aggregate Root)
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Events;
using eRaven.Domain.Exceptions;
using eRaven.Domain.Models;
using eRaven.Domain.Services;
using eRaven.Domain.ValueObjects;

namespace eRaven.Domain.Aggregates;

/// <summary>
/// Людина, основа для картки
/// </summary>
public class PersonAggregate
{
    // ==================== Identity ====================
    public Guid Id { get; set; }

    // ==================== Value Objects ====================
    public PersonalInfo PersonalInfo { get; private set; } = default!;
    public MilitaryDetails MilitaryDetails { get; private set; } = default!;

    // ==================== Current State ====================
    public int? StatusKindId { get; private set; }
    public Guid? PositionUnitId { get; private set; }

    // ==================== History (частина агрегату) ====================
    private readonly List<PersonStatus> _statusHistory = [];
    private readonly List<PersonPositionAssignment> _positionAssignments = [];
    private readonly List<PlanAction> _planActions = [];

    public IReadOnlyList<PersonStatus> StatusHistory => _statusHistory.AsReadOnly();
    public IReadOnlyList<PersonPositionAssignment> PositionAssignments => _positionAssignments.AsReadOnly();
    public IReadOnlyList<PlanAction> PlanActions => _planActions.AsReadOnly();

    // ==================== Domain Events ====================
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // ==================== Метадані ====================
    public DateTime CreatedUtc { get; private set; }
    public DateTime ModifiedUtc { get; private set; }

    // ==================== Constructors ====================

    // Для EF Core (приватний)
    private PersonAggregate() { }

    // Фабричний метод для створення нової особи
    public static PersonAggregate Create(
        PersonalInfo personalInfo,
        MilitaryDetails militaryDetails,
        int initialStatusKindId,
        IStatusTransitionValidator transitionValidator)
    {
        var person = new PersonAggregate
        {
            Id = Guid.NewGuid(),
            PersonalInfo = personalInfo,
            MilitaryDetails = militaryDetails,
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow
        };

        // Встановлюємо початковий статус (завжди "В районі")
        person.SetInitialStatus(initialStatusKindId, transitionValidator);

        person.AddDomainEvent(new PersonCreatedEvent(person.Id, personalInfo.FullName));
        return person;
    }

    // ==================== Бізнес-логіка статусів ====================

    private void SetInitialStatus(int statusKindId, IStatusTransitionValidator validator)
    {
        if (!validator.IsValidInitialStatus(statusKindId))
            throw new DomainException("Початковим статусом може бути лише 'В районі'");

        var status = new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = this.Id,
            StatusKindId = statusKindId,
            OpenDate = DateTime.UtcNow,
            Sequence = 0,
            IsActive = true,
            Author = "system",
            Modified = DateTime.UtcNow
        };

        _statusHistory.Add(status);
        StatusKindId = statusKindId;
        ModifiedUtc = DateTime.UtcNow;
    }

    public void ChangeStatus(
        int newStatusKindId,
        DateTime effectiveAtUtc,
        IStatusTransitionValidator transitionValidator,
        string? note = null,
        string? author = null)
    {
        // 1. Перевірка переходу
        if (!transitionValidator.IsTransitionAllowed(StatusKindId, newStatusKindId))
            throw new DomainException($"Перехід зі статусу {StatusKindId} до {newStatusKindId} заборонено");

        // 2. Перевірка часової послідовності
        var currentStatus = GetCurrentStatus();
        if (currentStatus != null && effectiveAtUtc < currentStatus.OpenDate)
            throw new DomainException("Момент має бути пізніший за останній відкритий статус");

        // 3. Визначаємо Sequence
        var sequence = CalculateNextSequence(effectiveAtUtc);

        // 4. Створюємо новий статус
        var status = new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = this.Id,
            StatusKindId = newStatusKindId,
            OpenDate = effectiveAtUtc,
            Sequence = sequence,
            IsActive = true,
            Note = note?.Trim(),
            Author = author?.Trim() ?? "system",
            Modified = DateTime.UtcNow
        };

        _statusHistory.Add(status);
        StatusKindId = newStatusKindId;
        ModifiedUtc = DateTime.UtcNow;

        AddDomainEvent(new PersonStatusChangedEvent(Id, newStatusKindId, effectiveAtUtc));
    }

    // ==================== Бізнес-логіка посад ====================

    public void AssignToPosition(
        Guid positionUnitId,
        DateTime openUtc,
        IPositionAssignmentPolicy policy,
        string? note = null)
    {
        // 1. Перевірка політики (посада активна і вільна)
        if (!policy.CanAssignToPosition(positionUnitId))
            throw new DomainException("Неможливо призначити на цю посаду");

        // 2. Закриваємо попереднє призначення
        var activeAssignment = GetActiveAssignment();
        if (activeAssignment != null)
        {
            if (activeAssignment.OpenUtc >= openUtc)
                throw new DomainException("Дата відкриття має бути пізніше за попереднє призначення");

            activeAssignment.CloseUtc = openUtc.AddDays(-1);
            activeAssignment.ModifiedUtc = DateTime.UtcNow;
        }

        // 3. Створюємо нове призначення
        var assignment = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = this.Id,
            PositionUnitId = positionUnitId,
            OpenUtc = openUtc,
            CloseUtc = null,
            Note = note?.Trim(),
            Author = "system",
            ModifiedUtc = DateTime.UtcNow
        };

        _positionAssignments.Add(assignment);
        PositionUnitId = positionUnitId;
        ModifiedUtc = DateTime.UtcNow;

        AddDomainEvent(new PersonAssignedToPositionEvent(Id, positionUnitId, openUtc));
    }

    public void UnassignFromPosition(DateTime closeUtc, string? note = null)
    {
        var activeAssignment = GetActiveAssignment()
            ?? throw new DomainException("Немає активного призначення");

        if (activeAssignment.OpenUtc >= closeUtc)
            throw new DomainException("Дата закриття має бути пізніше дати відкриття");

        activeAssignment.CloseUtc = closeUtc;
        activeAssignment.Note = note?.Trim();
        activeAssignment.ModifiedUtc = DateTime.UtcNow;

        PositionUnitId = null;
        ModifiedUtc = DateTime.UtcNow;

        AddDomainEvent(new PersonUnassignedFromPositionEvent(Id, closeUtc));
    }

    // ==================== Бізнес-логіка планових дій ====================

    public void CreatePlanAction(
        string planActionName,
        DateTime effectiveAtUtc,
        MoveType moveType,
        string location,
        string? groupName = null,
        string? crewName = null,
        string? note = null)
    {
        // Валідація: дата не може бути раніше останньої планової дії
        var lastAction = _planActions
            .OrderByDescending(a => a.EffectiveAtUtc)
            .FirstOrDefault();

        if (lastAction != null && effectiveAtUtc <= lastAction.EffectiveAtUtc)
            throw new DomainException("Дата планової дії має бути пізніше за останню");

        var action = new PlanAction
        {
            Id = Guid.NewGuid(),
            PersonId = Id,
            PlanActionName = planActionName,
            EffectiveAtUtc = effectiveAtUtc,
            ActionState = ActionState.PlanAction,
            MoveType = moveType,
            Location = location,
            GroupName = groupName!,
            CrewName = crewName!,
            Note = note ?? string.Empty,

            // Snapshot поточного стану
            Rnokpp = PersonalInfo.Rnokpp,
            FullName = PersonalInfo.FullName,
            RankName = MilitaryDetails.Rank,
            Callsign = MilitaryDetails.Callsign ?? string.Empty,
            BZVP = MilitaryDetails.BZVP,
            Weapon = MilitaryDetails.Weapon!
        };

        _planActions.Add(action);
        AddDomainEvent(new PlanActionCreatedEvent(Id, action.Id, effectiveAtUtc));
    }

    // ==================== Допоміжні методи ====================

    private PersonStatus? GetCurrentStatus() =>
        _statusHistory
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.OpenDate)
            .ThenByDescending(s => s.Sequence)
            .FirstOrDefault();

    private PersonPositionAssignment? GetActiveAssignment() =>
        _positionAssignments.FirstOrDefault(a => a.CloseUtc == null);

    private short CalculateNextSequence(DateTime openDate)
    {
        var maxSeq = _statusHistory
            .Where(s => s.IsActive && s.OpenDate == openDate)
            .Select(s => (short?)s.Sequence)
            .Max() ?? -1;
        return (short)(maxSeq + 1);
    }

    private void AddDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);

    public void ClearDomainEvents() => _domainEvents.Clear();

    // ==================== Оновлення інформації ====================

    public void UpdatePersonalInfo(PersonalInfo newInfo)
    {
        PersonalInfo = newInfo;
        ModifiedUtc = DateTime.UtcNow;
    }

    public void UpdateMilitaryDetails(MilitaryDetails newDetails)
    {
        MilitaryDetails = newDetails;
        ModifiedUtc = DateTime.UtcNow;
    }
}