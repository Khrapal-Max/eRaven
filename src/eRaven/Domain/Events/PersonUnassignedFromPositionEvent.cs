//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonUnassignedFromPositionEvent
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

public record PersonUnassignedFromPositionEvent(Guid PersonId, DateTime EffectiveAt) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}