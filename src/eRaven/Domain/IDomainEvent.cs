//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IDomainEvent
//-----------------------------------------------------------------------------

namespace eRaven.Domain;

public interface IDomainEvent
{
    Guid AggregateId { get; }
    string Author { get; }
    DateTime OccurredAtUtc { get; }
}
