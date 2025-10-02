//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonCreatedDomainEvent
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events.Integrations;

public record PersonCreatedDomainEvent(
    Guid PersonId,
    string FullName) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}