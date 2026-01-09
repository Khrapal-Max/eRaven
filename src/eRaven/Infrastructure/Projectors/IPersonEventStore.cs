//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonEventStore
//-----------------------------------------------------------------------------

using eRaven.Domain;

namespace eRaven.Infrastructure.Projectors;

public interface IPersonEventStore
{
    Task<bool> PersonExistsAsync(string rnokpp, CancellationToken ct = default);
    Task PersistAndProjectAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default);
}
