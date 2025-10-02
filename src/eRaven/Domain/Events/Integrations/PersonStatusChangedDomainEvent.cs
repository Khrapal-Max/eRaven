//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonStatusChangedDomainEvent
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events.Integrations;

public record PersonStatusChangedDomainEvent(
    Guid PersonId,
    int NewStatusKindId,
    DateTime EffectiveAt) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}