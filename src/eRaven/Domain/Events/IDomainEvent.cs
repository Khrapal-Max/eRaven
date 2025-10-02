//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IDomainEvent
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
