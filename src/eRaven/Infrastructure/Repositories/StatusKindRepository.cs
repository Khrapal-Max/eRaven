//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusKindRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Repositories;
using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories;

public sealed class StatusKindRepository(IDbContextFactory<AppDbContext> dbFactory) : IStatusKindRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public StatusKind? GetById(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.StatusKinds.FirstOrDefault(s => s.Id == id);
    }

    public async Task<IReadOnlyList<StatusKind>> GetAllAsync(
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.StatusKinds.ToListAsync(ct);
    }
}