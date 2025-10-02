//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActionCreatedEvent
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

public record PlanActionCreatedEvent(Guid PersonId, Guid ActionId, DateTime EffectiveAt) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
