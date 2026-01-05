//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonPositionChanged
//-----------------------------------------------------------------------------


namespace eRaven.Domain.Events;

/// <summary>
/// Зміна посади у персони.
/// </summary>
public sealed record PersonPositionChanged(
    Guid AggregateId,
    DateOnly EffectiveDate,
    string Position,
    string? Note,
    string Author,
    DateTime OccurredAtUtc) : IDomainEvent;
