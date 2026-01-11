//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonTemporaryPositionChanged
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events.PersonEvents;

/// <summary>
/// Призначення або зміна тимчасової посади працівника.
/// </summary>
public sealed record PersonTemporaryPositionChanged(
    Guid EventId,
    Guid AggregateId,
    DateOnly EffectiveDate,
    string? TemporaryPosition,
    Guid? TemporaryPositionUnitId,   // NEW
    string? Note,
    string Author,
    DateTime OccurredAtUtc
) : IDomainEvent;
