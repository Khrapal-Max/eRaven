//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonReadModelProjector
//-----------------------------------------------------------------------------


//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonReadModelProjector
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Projectors;

public interface IPersonReadModelProjector
{
    Task ProjectAsync(PersonEventRecord @event, CancellationToken ct = default);
}
