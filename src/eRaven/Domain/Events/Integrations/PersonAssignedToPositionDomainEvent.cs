//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonAssignedToPositionDomainEvent
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events.Integrations;

public record PersonAssignedToPositionDomainEvent(
    Guid PersonId,
    Guid PositionUnitId,
    DateTime EffectiveAt) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
