//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonCandidateCreated
//-----------------------------------------------------------------------------

using eRaven.Domain.ValueObjects;

namespace eRaven.Domain.Events.PersonEvents;

/// <summary>
/// Базова точка: створено кандидата + одразу заповнено персональні дані і планову посаду.
/// </summary>
public sealed record PersonCandidateCreated(
    Guid EventId,
    Guid AggregateId,
    PersonalInfo Personal,
    string? PlannedPosition,
    Guid? PlannedPositionUnitId,   // NEW
    string Author,
    DateTime OccurredAtUtc
) : IDomainEvent;
