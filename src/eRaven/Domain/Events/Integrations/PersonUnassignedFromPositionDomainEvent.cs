//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonUnassignedFromPositionDomainEvent
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events.Integrations;

public record PersonUnassignedFromPositionDomainEvent(
    Guid PersonId,
    DateTime EffectiveAt) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
