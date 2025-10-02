//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Person (Aggregate Root)
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Events;
using eRaven.Domain.Events.Integrations;
using eRaven.Domain.Exceptions;
using eRaven.Domain.Services;
using eRaven.Domain.ValueObjects;

namespace eRaven.Domain.Aggregates;

/// <summary>
/// Агрегат Особа - облік військовослужбовця через події
/// </summary>
public class PersonAggregate
{
    // ==================== Identity ====================
    public Guid Id { get; private set; }

    // ==================== Value Objects ====================
    public PersonalInfo PersonalInfo { get; private set; }
    public MilitaryDetails MilitaryDetails { get; private set; }

    // ==================== Current State (для оптимізації запитів) ====================
    public int? CurrentStatusKindId { get; private set; }
    public Guid? CurrentPositionUnitId { get; private set; }

    // ==================== Event History(частина агрегату) ====================
    private readonly List<StatusChangedEvent> _statusHistory = [];
    private readonly List<PositionAssignedEvent> _positionHistory = [];
    private readonly List<PlanActionRecordedEvent> _planActions = []; // ← RENAMED

    public IReadOnlyList<StatusChangedEvent> StatusHistory => _statusHistory.AsReadOnly();
    public IReadOnlyList<PositionAssignedEvent> PositionHistory => _positionHistory.AsReadOnly();
    public IReadOnlyList<PlanActionRecordedEvent> PlanActions => _planActions.AsReadOnly(); // ← RENAMED

    // ==================== Domain Events (для інтеграції) ====================
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // ==================== Метадані ====================
    public DateTime CreatedUtc { get; private set; }
    public DateTime ModifiedUtc { get; private set; }

    // ==================== Constructors ====================

    // EF Core constructor
    private PersonAggregate()
    {
        PersonalInfo = null!;
        MilitaryDetails = null!;
    }

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

        person.SetInitialStatus(initialStatusKindId, transitionValidator);

        // Integration event
        person.AddDomainEvent(new PersonCreatedDomainEvent(person.Id, personalInfo.FullName));
        return person;
    }

    // ==================== Бізнес-логіка статусів ====================

    private void SetInitialStatus(int statusKindId, IStatusTransitionValidator validator)
    {
        if (!validator.IsValidInitialStatus(statusKindId))
            throw new DomainException("Початковим статусом може бути лише 'В районі' (код 30)");

        var statusEvent = new StatusChangedEvent(
            personId: Id,
            statusKindId: statusKindId,
            effectiveAt: DateTime.UtcNow,
            sequence: 0,
            note: "Початковий статус",
            author: "system"
        );

        _statusHistory.Add(statusEvent);
        CurrentStatusKindId = statusKindId;
    }

    public void ChangeStatus(
         int newStatusKindId,
         DateTime effectiveAtUtc,
         IStatusTransitionValidator transitionValidator,
         HashSet<int> allowedTransitions,
         string? note = null,
         string author = "system",
         Guid? sourceDocumentId = null,
         string? sourceDocumentType = null)
    {
        // 1. Валідація переходу
        if (!transitionValidator.IsTransitionAllowed(CurrentStatusKindId, newStatusKindId, allowedTransitions))
            throw new DomainException($"Перехід зі статусу {CurrentStatusKindId} до {newStatusKindId} заборонено");

        // 2. Перевірка часової послідовності
        var lastEvent = GetLastStatusEvent();
        if (lastEvent != null && effectiveAtUtc < lastEvent.EffectiveAt)
            throw new DomainException("Момент має бути пізніший за останній відкритий статус");

        // 3. Визначаємо Sequence
        var sequence = CalculateNextSequence(effectiveAtUtc);

        // 4. Створюємо подію
        var statusEvent = new StatusChangedEvent(
           personId: Id,
           statusKindId: newStatusKindId,
           effectiveAt: effectiveAtUtc,
           sequence: CalculateNextSequence(effectiveAtUtc),
           note: note,
           author: author,
           sourceDocumentId: sourceDocumentId,
           sourceDocumentType: sourceDocumentType
       );

        _statusHistory.Add(statusEvent);
        CurrentStatusKindId = newStatusKindId;
        ModifiedUtc = DateTime.UtcNow;

        // Integration event
        AddDomainEvent(new PersonStatusChangedDomainEvent(Id, newStatusKindId, effectiveAtUtc));
    }

    // ==================== Бізнес-логіка посад ====================

    public void AssignToPosition(
       Guid positionUnitId,
       DateTime openUtc,
       IPositionAssignmentPolicy policy,
       string? note = null,
       string author = "system")
    {
        // 1. Перевірка політики
        if (!policy.CanAssignToPosition(positionUnitId))
            throw new DomainException("Неможливо призначити на цю посаду (вона зайнята або неактивна)");

        // 2. Закриваємо попереднє призначення
        var activeAssignment = GetActivePositionAssignment();
        if (activeAssignment != null)
        {
            if (activeAssignment.OpenUtc >= openUtc)
                throw new DomainException("Дата відкриття має бути пізніше за попереднє призначення");

            activeAssignment.Close(openUtc.AddDays(-1), "Автоматично закрито при новому призначенні");
        }

        // Aggregate internal event
        var assignmentEvent = new PositionAssignedEvent(
            personId: Id,
            positionUnitId: positionUnitId,
            openUtc: openUtc,
            note: note,
            author: author
        );

        _positionHistory.Add(assignmentEvent);
        CurrentPositionUnitId = positionUnitId;
        ModifiedUtc = DateTime.UtcNow;

        // Integration event
        AddDomainEvent(new PersonAssignedToPositionDomainEvent(Id, positionUnitId, openUtc));
    }

    public void UnassignFromPosition(DateTime closeUtc, string? note = null)
    {
        var activeAssignment = GetActivePositionAssignment()
            ?? throw new DomainException("Немає активного призначення");

        activeAssignment.Close(closeUtc, note);
        CurrentPositionUnitId = null;
        ModifiedUtc = DateTime.UtcNow;

        // Integration event
        AddDomainEvent(new PersonUnassignedFromPositionDomainEvent(Id, closeUtc));
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

        // Створюємо snapshot поточного стану
        var activePosition = GetActivePositionAssignment();
        var lastStatus = GetLastStatusEvent();

        // Aggregate internal event (with full snapshot)
        var planAction = new PlanActionRecordedEvent( // ← RENAMED
            personId: Id,
            planActionName: planActionName,
            effectiveAtUtc: effectiveAtUtc,
            moveType: moveType,
            location: location,
            groupName: groupName,
            crewName: crewName,
            note: note,
            // Snapshot
            rnokpp: PersonalInfo.Rnokpp,
            fullName: PersonalInfo.FullName,
            rankName: MilitaryDetails.Rank,
            callsign: MilitaryDetails.Callsign,
            bzvp: MilitaryDetails.BZVP,
            weapon: MilitaryDetails.Weapon,
            positionName: activePosition != null ? $"Посада {activePosition.PositionUnitId}" : "Без посади",
            statusKindOnDate: lastStatus != null ? $"Статус {lastStatus.StatusKindId}" : "Невідомо"
        );

        _planActions.Add(planAction);

        // Integration event (minimal data)
        AddDomainEvent(new PlanActionCreatedDomainEvent(
            PersonId: Id,
            PlanActionId: planAction.Id,
            EffectiveAt: effectiveAtUtc,
            MoveType: moveType
        ));
    }

    public void ApprovePlanAction(Guid planActionId, string order)
    {
        var planAction = _planActions.FirstOrDefault(a => a.Id == planActionId)
            ?? throw new DomainException("Планова дія не знайдена");

        planAction.Approve(order);
        ModifiedUtc = DateTime.UtcNow;
    }

    // ==================== Допоміжні методи ====================

    private StatusChangedEvent? GetLastStatusEvent() =>
        _statusHistory
            .OrderByDescending(s => s.EffectiveAt)
            .ThenByDescending(s => s.Sequence)
            .FirstOrDefault();

    private PositionAssignedEvent? GetActivePositionAssignment() =>
        _positionHistory.FirstOrDefault(a => a.CloseUtc == null);

    private short CalculateNextSequence(DateTime effectiveAt)
    {
        var maxSeq = _statusHistory
            .Where(s => s.EffectiveAt == effectiveAt)
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