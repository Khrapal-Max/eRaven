//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonReadModelProjector
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Projections.Person;

public interface IPersonReadModelProjector
{
    Task ProjectAsync(AppDbContext db, PersonEventRecord record, CancellationToken ct = default);
    Task RebuildAsync(AppDbContext db, Guid aggregateId, CancellationToken ct = default);
}
