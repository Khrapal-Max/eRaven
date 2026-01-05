//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonCallsignChanged
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

/// <summary>
/// Зміна позивного у персони.
/// </summary>
public sealed record PersonCallsignChanged(
    Guid AggregateId,
    DateOnly EffectiveDate,
    string? Callsign,
    string Author,
    DateTime OccurredAtUtc) : IDomainEvent;
