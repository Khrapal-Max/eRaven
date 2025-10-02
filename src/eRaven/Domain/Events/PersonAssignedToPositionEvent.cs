//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonAssignedToPositionEvent
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

public record PersonAssignedToPositionEvent(Guid PersonId, Guid PositionUnitId, DateTime EffectiveAt) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}