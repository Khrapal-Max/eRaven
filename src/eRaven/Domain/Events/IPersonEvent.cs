//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;

namespace eRaven.Domain.Events;

/// <summary>
/// Базовий інтерфейс доменних подій агрегату <see cref="Person"/>.
/// </summary>
public interface IPersonEvent
{
    Guid PersonId { get; }

    /// <summary>
    /// Момент часу (UTC), коли сталася подія.
    /// </summary>
    DateTime OccurredAtUtc { get; }
}

public sealed record PersonCreatedEvent(
    Guid PersonId,
    string Rnokpp,
    string Rank,
    string LastName,
    string FirstName,
    string? MiddleName,
    string BZVP,
    string? Weapon,
    string? Callsign,
    bool IsAttached,
    string? AttachedFromUnit,
    DateTime OccurredAtUtc) : IPersonEvent;

public sealed record PersonPositionAssignedEvent(
    Guid PersonId,
    Guid AssignmentId,
    Guid PositionUnitId,
    DateTime OccurredAtUtc,
    string? Note,
    string? Author) : IPersonEvent;

public sealed record PersonPositionRemovedEvent(
    Guid PersonId,
    Guid AssignmentId,
    DateTime OccurredAtUtc,
    string? Note,
    string? Author) : IPersonEvent;

public sealed record PersonPositionAssignmentTouchedEvent(
    Guid PersonId,
    Guid AssignmentId,
    DateTime OccurredAtUtc,
    string? Note,
    string? Author) : IPersonEvent;

public sealed record PersonStatusSetEvent(
    Guid PersonId,
    Guid StatusId,
    int StatusKindId,
    DateTime OccurredAtUtc,
    short Sequence,
    string? Note,
    string? Author,
    Guid? SourceDocumentId,
    string? SourceDocumentType) : IPersonEvent;

public sealed record PersonStatusNoteUpdatedEvent(
    Guid PersonId,
    Guid StatusId,
    DateTime OccurredAtUtc,
    string? Note) : IPersonEvent;

public sealed record PersonStatusClearedEvent(
    Guid PersonId,
    Guid StatusId,
    DateTime OccurredAtUtc,
    string? Author) : IPersonEvent;

public sealed record PlanActionAddedEvent(
    Guid PersonId,
    Guid PlanActionId,
    string PlanActionName,
    DateTime OccurredAtUtc,
    int? ToStatusKindId,
    string? Order,
    ActionState ActionState,
    MoveType MoveType,
    string Location,
    string GroupName,
    string CrewName,
    string Note,
    string Rnokpp,
    string FullName,
    string RankName,
    string PositionName,
    string BZVP,
    string Weapon,
    string Callsign,
    string StatusKindOnDate) : IPersonEvent;
