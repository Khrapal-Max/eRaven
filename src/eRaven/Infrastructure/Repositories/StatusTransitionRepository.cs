//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitionRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories;

public sealed class StatusTransitionRepository(IDbContextFactory<AppDbContext> dbFactory) : IStatusTransitionRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public bool IsTransitionAllowed(int fromStatusKindId, int toStatusKindId)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.StatusTransitions
            .Any(t => t.FromStatusKindId == fromStatusKindId &&
                      t.ToStatusKindId == toStatusKindId);
    }

    public async Task<HashSet<int>> GetAllowedToStatusesAsync(
        int fromStatusKindId,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var ids = await db.StatusTransitions
            .Where(t => t.FromStatusKindId == fromStatusKindId)
            .Select(t => t.ToStatusKindId)
            .ToListAsync(ct);

        return ids.ToHashSet();
    }
}