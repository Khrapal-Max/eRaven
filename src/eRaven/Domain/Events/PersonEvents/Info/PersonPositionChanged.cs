//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonPositionChanged
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events.PersonEvents.Info;

/// <summary>
/// Зміна посади у персони.
/// </summary>
public sealed record PersonPositionChanged(
    Guid EventId,
    Guid AggregateId,
    DateOnly EffectiveDate,
    int PositionSort,
    string? Position,         // <-- можна null щоб "очистити"
    string? Note,
    string Author,
    DateTime OccurredAtUtc
) : IDomainEvent;
