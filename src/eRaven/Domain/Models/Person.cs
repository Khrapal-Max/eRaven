//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Person (Aggregate Root)
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace eRaven.Domain.Models;

/// <summary>
/// Людина — агрегат, який містить картку, призначення на посади та історію статусів.
/// </summary>
public class Person
{
    private readonly List<PersonStatus> _statusHistory = [];
    private readonly List<PersonPositionAssignment> _positionAssignments = [];
    private readonly List<PlanAction> _planActions = [];

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

        if (CurrentAssignment is { IsActive: true } activeAssignment)
        {
            if (activeAssignment.PositionUnitId == position.Id)
            {
                activeAssignment.Touch(utc, note, author);
                PositionUnitId = position.Id;
                PositionUnit = position;
                ModifiedUtc = utc;
                return;
            }

            activeAssignment.Close(utc, note, author);
        }

        var snapshot = PersonPositionAssignment.Create(Id, position.Id, utc, note, author);
        _positionAssignments.Add(snapshot);

        PositionUnitId = position.Id;
        PositionUnit = position;
        ModifiedUtc = utc;
    }

    /// <summary>
    /// Знімає людину з поточної посади.
    /// </summary>
    public void RemoveFromPosition(DateTime removedAtUtc, string? note, string? author)
    {
        var activeAssignment = CurrentAssignment;
        if (activeAssignment is null)
        {
            throw new InvalidOperationException("Людина не має активного призначення.");
        }

        var utc = EnsureUtc(removedAtUtc);
        activeAssignment.Close(utc, note, author);

        PositionUnitId = null;
        PositionUnit = null;
        ModifiedUtc = utc;
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
                current.UpdateNote(note, utc);
                ModifiedUtc = utc;
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

            if (current.IsActive)
            {
                current.Close(utc, author);
            }
        }

        var nextSequence = (short)(_statusHistory.Count == 0 ? 1 : _statusHistory.Max(s => s.Sequence) + 1);
        var snapshot = PersonStatus.Create(Id, statusKind.Id, utc, nextSequence, note, author, sourceDocumentId, sourceDocumentType);
        _statusHistory.Add(snapshot);

        StatusKindId = statusKind.Id;
        StatusKind = statusKind;
        ModifiedUtc = utc;
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
        if (current.IsActive)
        {
            current.Close(utc, author);
        }

        StatusKindId = null;
        StatusKind = null;
        ModifiedUtc = utc;
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

        _planActions.Add(snapshot);
        ModifiedUtc = snapshot.EffectiveAtUtc;
        return snapshot;
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
