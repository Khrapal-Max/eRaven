//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonPositionChanged
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events.PersonEvents;

/// <summary>
/// Зміна посади у персони.
/// </summary>
public sealed record PersonPositionChanged(
    Guid EventId,
    Guid AggregateId,
    DateOnly EffectiveDate,
    Guid PositionUnitId,        // ✅ NEW
    string Position,            // (можно оставить для снапшота/читаемости)
    string? Note,
    string Author,
    DateTime OccurredAtUtc
) : IDomainEvent;
