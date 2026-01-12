//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonBzvpChanged
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events.PersonEvents.Info;

/// <summary>
/// Зміна БЗВП у персони.
/// </summary>
public sealed record PersonBzvpChanged(
    Guid EventId,
    Guid AggregateId,
    DateOnly EffectiveDate,
    string Bzvp,
    string? Note,
    string Author,
    DateTime OccurredAtUtc) : IDomainEvent;
