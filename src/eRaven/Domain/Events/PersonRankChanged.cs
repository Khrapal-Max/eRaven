//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonRankChanged
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

/// <summary>
/// Зміна звання у персони.
/// </summary>
public sealed record PersonRankChanged(
    Guid EventId,
    Guid AggregateId,
    DateOnly EffectiveDate,
    string Rank,
    string? Note,
    string Author,
    DateTime OccurredAtUtc) : IDomainEvent;
