//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonExcluded
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

/// <summary>
/// Подія виключення особи
/// </summary>
public sealed record PersonExcluded(
    Guid EventId,
    Guid AggregateId,
    string Reason,
    DateOnly EffectiveDate,
    string Author,
    DateTime OccurredAtUtc) : IDomainEvent;
