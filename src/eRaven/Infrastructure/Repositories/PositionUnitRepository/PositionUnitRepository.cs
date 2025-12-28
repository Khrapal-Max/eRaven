//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.PositionUnitRepository;

public class PositionUnitRepository(IDbContextFactory<AppDbContext> dbFactory) : IPositionUnitRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <summary>
    /// Повернення всіх активних посад
    /// </summary>
    /// <param name="ct"></param>
    /// <returns>IEnumerable PositionUnit<see cref="PositionUnit"/></returns>
    public async Task<IEnumerable<PositionUnit>> GetAllPositionUnits(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.PositionUnits
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Додавання посади
    /// </summary>
    /// <param name="positionUnit"></param>
    /// <param name="ct"></param>
    /// <returns>Task</returns>
    public async Task AddPositionUnit(PositionUnit positionUnit, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        await db.PositionUnits.AddAsync(positionUnit, ct);

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Деактивация посади
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns>Task</returns>
    public async Task DeActivatedPositionUnit(Guid id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var position = await db.PositionUnits.FirstOrDefaultAsync(x => x.Id == id, ct);

        position!.IsActived = false;

        await db.SaveChangesAsync(ct);
    }
}
