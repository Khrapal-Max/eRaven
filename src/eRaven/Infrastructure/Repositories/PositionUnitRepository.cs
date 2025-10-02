//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Repositories;
using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories;

public sealed class PositionUnitRepository(IDbContextFactory<AppDbContext> dbFactory) : IPositionUnitRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public PositionUnit? GetById(Guid id)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.PositionUnits.FirstOrDefault(p => p.Id == id);
    }

    public async Task<IReadOnlyList<PositionUnit>> GetAllActiveAsync(
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.PositionUnits
            .Where(p => p.IsActived)
            .ToListAsync(ct);
    }
}
