//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IStatusTransitionRepository Application layer
//-----------------------------------------------------------------------------

namespace eRaven.Application.Repositories;

public interface IStatusTransitionRepository
{
    bool IsTransitionAllowed(int fromStatusKindId, int toStatusKindId);

    Task<HashSet<int>> GetAllowedToStatusesAsync(int fromStatusKindId, CancellationToken ct = default);
}
