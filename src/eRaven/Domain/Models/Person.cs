//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Person (Aggregate Root)
//-----------------------------------------------------------------------------

using eRaven.Domain.Events;
using eRaven.Domain.Models.Projections;

namespace eRaven.Domain.Models;

/// <summary>
/// Людина — агрегат, який містить картку, призначення на посади та історію статусів.
/// </summary>
public class Person
{
    private readonly List<PersonStatus> _statusHistory = [];
    private readonly List<PersonPositionAssignment> _positionAssignments = [];
    private readonly List<PlanAction> _planActions = [];
    private readonly List<IPersonEvent> _events = [];

    public Guid Id { get; set; }

    /// <summary>
    /// ІПН
    /// </summary>
    public string Rnokpp { get; set; } = string.Empty;

    /// <summary>
    /// Звання
    /// </summary>
    public string Rank { get; set; } = string.Empty;

    /// <summary>
    /// Прізвище
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Ім'я
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// По батькові
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Наявність юазовго навчанян
    /// </summary>
    public string BZVP { get; set; } = string.Empty;

    /// <summary>
    /// Тип та номер зброї
    /// </summary>
    public string? Weapon { get; set; }

    /// <summary>
    /// Позивний
    /// </summary>
    public string? Callsign { get; set; }

    /// <summary>
    /// Поточна посада з довідника.
    /// </summary>
    public Guid? PositionUnitId { get; set; }
    public PositionUnit? PositionUnit { get; set; }

    /// <summary>
    /// Поточний статус - наприклад "В районі" або "СЗЧ"
    /// </summary>
    public int? StatusKindId { get; set; }
    public StatusKind? StatusKind { get; set; }

    /// <summary>
    /// Придані/прибули
    /// </summary>
    public bool IsAttached { get; set; }               // свої=false, прибули=true
    public string? AttachedFromUnit { get; set; }      // назва/шлях підрозділу походження (для прибулих)

    /// <summary>
    /// Час створення
    /// </summary>
    public DateTime CreatedUtc { get; set; }           // UTC

    /// <summary>
    /// Час останньої зміни
    /// </summary>
    public DateTime ModifiedUtc { get; set; }

    /// <summary>
    /// Історія зміни статусів.
    /// </summary>
    public IReadOnlyCollection<PersonStatus> StatusHistory => _statusHistory.AsReadOnly();

    /// <summary>
    /// Історія призначень на посади.
    /// </summary>
    public IReadOnlyCollection<PersonPositionAssignment> PositionAssignments => _positionAssignments.AsReadOnly();

    /// <summary>
    /// Історія планових дій.
    /// </summary>
    public IReadOnlyCollection<PlanAction> PlanActions => _planActions.AsReadOnly();

    /// <summary>
    /// Незбережені доменні події.
    /// </summary>
    public IReadOnlyCollection<IPersonEvent> PendingEvents => _events.AsReadOnly();

    /// <summary>
    /// Конкатенація повного імені
    /// </summary>
    public string FullName =>
        string.Join(" ", new[] { LastName, FirstName, MiddleName }.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>
    /// Поточне активне призначення.
    /// </summary>
    public PersonPositionAssignment? CurrentAssignment =>
        _positionAssignments.FirstOrDefault(a => a.IsActive);

    /// <summary>
    /// Поточний активний статус.
    /// </summary>
    public PersonStatus? CurrentStatus =>
        _statusHistory.OrderByDescending(s => s.Sequence).FirstOrDefault(s => s.IsActive);

    /// <summary>
    /// Призначає людину на посаду.
    /// </summary>
    public void AssignToPosition(PositionUnit position, DateTime assignedAtUtc, string? note, string? author)
    {
        ArgumentNullException.ThrowIfNull(position);

        var utc = EnsureUtc(assignedAtUtc);
        var activeAssignment = CurrentAssignment;

        if (activeAssignment is { IsActive: true })
        {
            if (activeAssignment.PositionUnitId == position.Id)
            {
                Record(new PersonPositionAssignmentTouchedEvent(Id, activeAssignment.Id, utc, note, author));
                PositionUnit = position;
                return;
            }

            Record(new PersonPositionRemovedEvent(Id, activeAssignment.Id, utc, note, author));
        }

        var assignmentId = Guid.NewGuid();
        Record(new PersonPositionAssignedEvent(Id, assignmentId, position.Id, utc, note, author));

        PositionUnit = position;
    }

    /// <summary>
    /// Знімає людину з поточної посади.
    /// </summary>
    public void RemoveFromPosition(DateTime removedAtUtc, string? note, string? author)
    {

        var activeAssignment = CurrentAssignment ?? throw new InvalidOperationException("Людина не має активного призначення.");

        var utc = EnsureUtc(removedAtUtc);
        Record(new PersonPositionRemovedEvent(Id, activeAssignment.Id, utc, note, author));

        PositionUnit = null;
    }

    /// <summary>
    /// Встановлює статус з перевіркою дозволених переходів.
    /// </summary>
    public void SetStatus(
        StatusKind statusKind,
        DateTime effectiveAtUtc,
        string? note,
        string? author,
        IEnumerable<StatusTransition>? transitions,
        Guid? sourceDocumentId = null,
        string? sourceDocumentType = null)
    {
        ArgumentNullException.ThrowIfNull(statusKind);

        var utc = EnsureUtc(effectiveAtUtc);
        var current = CurrentStatus;

        if (current is not null)
        {
            if (current.StatusKindId == statusKind.Id)
            {
                Record(new PersonStatusNoteUpdatedEvent(Id, current.Id, utc, note));
                StatusKind = statusKind;
                return;
            }

            if (transitions is not null && current.IsActive)
            {
                var allowed = transitions.Any(t => t.FromStatusKindId == current.StatusKindId && t.ToStatusKindId == statusKind.Id);
                if (!allowed)
                {
                    throw new InvalidOperationException(
                        $"Перехід зі статусу {current.StatusKindId} до {statusKind.Id} заборонено правилами.");
                }
            }
        }

        var nextSequence = (short)(_statusHistory.Count == 0 ? 1 : _statusHistory.Max(s => s.Sequence) + 1);
        var statusId = Guid.NewGuid();
        Record(new PersonStatusSetEvent(
            Id,
            statusId,
            statusKind.Id,
            utc,
            nextSequence,
            note,
            author,
            sourceDocumentId,
            sourceDocumentType));

        StatusKind = statusKind;
    }

    /// <summary>
    /// Скидає поточний статус.
    /// </summary>
    public void ClearStatus(DateTime clearedAtUtc, string? author)
    {
        var current = CurrentStatus;
        if (current is null)
        {
            return;
        }

        var utc = EnsureUtc(clearedAtUtc);
        Record(new PersonStatusClearedEvent(Id, current.Id, utc, author));

        StatusKind = null;
    }

    /// <summary>
    /// Фіксує планову дію.
    /// </summary>
    public PlanAction AddPlanAction(PlanAction snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.PersonId != Id)
        {
            throw new InvalidOperationException("Снапшот не належить цій людині.");
        }

        var planActionId = snapshot.Id == Guid.Empty ? Guid.NewGuid() : snapshot.Id;
        snapshot.Id = planActionId;
        var effectiveAtUtc = EnsureUtc(snapshot.EffectiveAtUtc);
        snapshot.EffectiveAtUtc = effectiveAtUtc;

        Record(new PlanActionAddedEvent(
            snapshot.PersonId,
            planActionId,
            snapshot.PlanActionName,
            effectiveAtUtc,
            snapshot.ToStatusKindId,
            snapshot.Order,
            snapshot.ActionState,
            snapshot.MoveType,
            snapshot.Location,
            snapshot.GroupName,
            snapshot.CrewName,
            snapshot.Note,
            snapshot.Rnokpp,
            snapshot.FullName,
            snapshot.RankName,
            snapshot.PositionName,
            snapshot.BZVP,
            snapshot.Weapon,
            snapshot.Callsign,
            snapshot.StatusKindOnDate));

        return _planActions.Single(pa => pa.Id == planActionId);
    }

    /// <summary>
    /// Створює прожекцію картки людини.
    /// </summary>
    public PersonCardProjection ToCardProjection()
    {
        return new PersonCardProjection(
            Id,
            Rnokpp,
            Rank,
            LastName,
            FirstName,
            MiddleName,
            FullName,
            PositionUnitId,
            StatusKindId,
            IsAttached,
            AttachedFromUnit,
            CreatedUtc,
            ModifiedUtc);
    }

    /// <summary>
    /// Проекція історії призначень.
    /// </summary>
    public IReadOnlyList<PersonAssignmentProjection> BuildAssignmentsProjection()
    {
        return [.. _positionAssignments
            .OrderBy(a => a.OpenUtc)
            .Select(a => new PersonAssignmentProjection(
                a.Id,
                a.PositionUnitId,
                a.OpenUtc,
                a.CloseUtc,
                a.Note,
                a.Author))];
    }

    /// <summary>
    /// Проекція історії статусів.
    /// </summary>
    public IReadOnlyList<PersonStatusProjection> BuildStatusProjection()
    {
        return [.. _statusHistory
            .OrderBy(s => s.Sequence)
            .Select(s => new PersonStatusProjection(
                s.Id,
                s.StatusKindId,
                s.OpenDate,
                s.IsActive,
                s.Sequence,
                s.Note,
                s.Author,
                s.SourceDocumentId,
                s.SourceDocumentType))];
    }

    /// <summary>
    /// Проекція планових дій.
    /// </summary>
    public IReadOnlyList<PersonPlanActionProjection> BuildPlanActionsProjection()
    {
        return [.. _planActions
            .OrderBy(a => a.EffectiveAtUtc)
            .Select(a => new PersonPlanActionProjection(
                a.Id,
                a.PlanActionName,
                a.EffectiveAtUtc,
                a.ToStatusKindId,
                a.Order,
                a.ActionState,
                a.MoveType,
                a.Location,
                a.GroupName,
                a.CrewName,
                a.Note,
                a.Rnokpp,
                a.FullName,
                a.RankName,
                a.PositionName,
                a.BZVP,
                a.Weapon,
                a.Callsign,
                a.StatusKindOnDate))];
    }

    /// <summary>
    /// Очищує накопичені події.
    /// </summary>
    public void ClearPendingEvents() => _events.Clear();

    /// <summary>
    /// Створює людину та публікує подію створення.
    /// </summary>
    public static Person Create(
        Guid id,
        string rnokpp,
        string rank,
        string lastName,
        string firstName,
        string? middleName,
        string bzvp,
        string? weapon,
        string? callsign,
        bool isAttached,
        string? attachedFromUnit,
        DateTime createdUtc)
    {
        var person = new Person();
        var utc = EnsureUtc(createdUtc);

        person.Record(new PersonCreatedEvent(
            id,
            rnokpp,
            rank,
            lastName,
            firstName,
            middleName,
            bzvp,
            weapon,
            callsign,
            isAttached,
            attachedFromUnit,
            utc));

        return person;
    }

    /// <summary>
    /// Відбудовує агрегат із історії подій.
    /// </summary>
    public static Person BuildFromHistory(Guid id, IEnumerable<IPersonEvent> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        var person = new Person { Id = id };
        foreach (var @event in history.OrderBy(e => e.OccurredAtUtc))
        {
            if (@event.PersonId != id)
            {
                throw new InvalidOperationException("Подія належить іншій людині.");
            }

            person.When(@event);
        }

        return person;
    }

    private void Record(IPersonEvent @event)
    {
        When(@event);
        _events.Add(@event);
    }

    private void When(IPersonEvent @event)
    {
        switch (@event)
        {
            case PersonCreatedEvent created:
                Apply(created);
                break;
            case PersonPositionAssignedEvent assigned:
                Apply(assigned);
                break;
            case PersonPositionRemovedEvent removed:
                Apply(removed);
                break;
            case PersonPositionAssignmentTouchedEvent touched:
                Apply(touched);
                break;
            case PersonStatusSetEvent statusSet:
                Apply(statusSet);
                break;
            case PersonStatusNoteUpdatedEvent statusNoteUpdated:
                Apply(statusNoteUpdated);
                break;
            case PersonStatusClearedEvent statusCleared:
                Apply(statusCleared);
                break;
            case PlanActionAddedEvent planActionAdded:
                Apply(planActionAdded);
                break;
            default:
                throw new InvalidOperationException($"Невідомий тип події {@event.GetType().Name}.");
        }
    }

    private void Apply(PersonCreatedEvent @event)
    {
        Id = @event.PersonId;
        Rnokpp = @event.Rnokpp;
        Rank = @event.Rank;
        LastName = @event.LastName;
        FirstName = @event.FirstName;
        MiddleName = @event.MiddleName;
        BZVP = @event.BZVP;
        Weapon = @event.Weapon;
        Callsign = @event.Callsign;
        IsAttached = @event.IsAttached;
        AttachedFromUnit = @event.AttachedFromUnit;
        CreatedUtc = @event.OccurredAtUtc;
        ModifiedUtc = @event.OccurredAtUtc;
    }

    private void Apply(PersonPositionAssignedEvent @event)
    {
        var snapshot = new PersonPositionAssignment
        {
            Id = @event.AssignmentId,
            PersonId = @event.PersonId,
            PositionUnitId = @event.PositionUnitId,
            OpenUtc = @event.OccurredAtUtc,
            Note = @event.Note,
            Author = @event.Author,
            ModifiedUtc = @event.OccurredAtUtc
        };

        _positionAssignments.Add(snapshot);
        PositionUnitId = @event.PositionUnitId;
        if (PositionUnit is not null && PositionUnit.Id != @event.PositionUnitId)
        {
            PositionUnit = null;
        }

        ModifiedUtc = @event.OccurredAtUtc;
    }

    private void Apply(PersonPositionRemovedEvent @event)
    {
        var assignment = _positionAssignments.FirstOrDefault(a => a.Id == @event.AssignmentId)
            ?? throw new InvalidOperationException("Не знайдено призначення для закриття.");

        assignment.Close(@event.OccurredAtUtc, @event.Note, @event.Author);
        PositionUnitId = null;
        PositionUnit = null;
        ModifiedUtc = @event.OccurredAtUtc;
    }

    private void Apply(PersonPositionAssignmentTouchedEvent @event)
    {
        var assignment = _positionAssignments.FirstOrDefault(a => a.Id == @event.AssignmentId)
            ?? throw new InvalidOperationException("Не знайдено призначення для оновлення.");

        assignment.Touch(@event.OccurredAtUtc, @event.Note, @event.Author);
        PositionUnitId = assignment.PositionUnitId;
        ModifiedUtc = @event.OccurredAtUtc;
    }

    private void Apply(PersonStatusSetEvent @event)
    {
        var current = CurrentStatus;
        if (current is not null && current.IsActive)
        {
            current.Close(@event.OccurredAtUtc, @event.Author);
        }

        var snapshot = new PersonStatus
        {
            Id = @event.StatusId,
            PersonId = @event.PersonId,
            StatusKindId = @event.StatusKindId,
            OpenDate = @event.OccurredAtUtc,
            IsActive = true,
            Sequence = @event.Sequence,
            Note = @event.Note,
            Author = @event.Author,
            Modified = @event.OccurredAtUtc,
            SourceDocumentId = @event.SourceDocumentId,
            SourceDocumentType = @event.SourceDocumentType
        };

        _statusHistory.Add(snapshot);
        StatusKindId = @event.StatusKindId;
        if (StatusKind is not null && StatusKind.Id != @event.StatusKindId)
        {
            StatusKind = null;
        }

        ModifiedUtc = @event.OccurredAtUtc;
    }

    private void Apply(PersonStatusNoteUpdatedEvent @event)
    {
        var status = _statusHistory.FirstOrDefault(s => s.Id == @event.StatusId)
            ?? throw new InvalidOperationException("Не знайдено статус для оновлення нотатки.");

        status.UpdateNote(@event.Note, @event.OccurredAtUtc);
        ModifiedUtc = @event.OccurredAtUtc;
    }

    private void Apply(PersonStatusClearedEvent @event)
    {
        var current = CurrentStatus;
        if (current is not null && current.Id == @event.StatusId && current.IsActive)
        {
            current.Close(@event.OccurredAtUtc, @event.Author);
        }

        StatusKindId = null;
        StatusKind = null;
        ModifiedUtc = @event.OccurredAtUtc;
    }

    private void Apply(PlanActionAddedEvent @event)
    {
        var snapshot = new PlanAction
        {
            Id = @event.PlanActionId,
            PersonId = @event.PersonId,
            PlanActionName = @event.PlanActionName,
            EffectiveAtUtc = @event.OccurredAtUtc,
            ToStatusKindId = @event.ToStatusKindId,
            Order = @event.Order,
            ActionState = @event.ActionState,
            MoveType = @event.MoveType,
            Location = @event.Location,
            GroupName = @event.GroupName,
            CrewName = @event.CrewName,
            Note = @event.Note,
            Rnokpp = @event.Rnokpp,
            FullName = @event.FullName,
            RankName = @event.RankName,
            PositionName = @event.PositionName,
            BZVP = @event.BZVP,
            Weapon = @event.Weapon,
            Callsign = @event.Callsign,
            StatusKindOnDate = @event.StatusKindOnDate
        };

        var existing = _planActions.FirstOrDefault(a => a.Id == snapshot.Id);
        if (existing is not null)
        {
            _planActions.Remove(existing);
        }

        _planActions.Add(snapshot);
        ModifiedUtc = @event.OccurredAtUtc;
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => value
        };
    }
}
