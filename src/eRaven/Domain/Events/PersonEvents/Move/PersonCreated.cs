//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonCreated
//-----------------------------------------------------------------------------

using eRaven.Domain.ValueObjects;

namespace eRaven.Domain.Events.PersonEvents.Move;

/// <summary>
/// Базова точка: створено картку.
/// </summary>
public sealed record PersonCreated(
    Guid EventId,
    Guid AggregateId,
    PersonalInfo Personal,
    string? Rank,
    string? Position,
    string Author,
    DateTime OccurredAtUtc
) : IDomainEvent;
