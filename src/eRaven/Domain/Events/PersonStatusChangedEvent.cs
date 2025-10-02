//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonStatusChangedEvent
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

public record PersonStatusChangedEvent(Guid PersonId, int NewStatusKindId, DateTime EffectiveAt) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
