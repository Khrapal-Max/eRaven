//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonCreatedEvent
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

public record PersonCreatedEvent(Guid PersonId, string FullName) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
