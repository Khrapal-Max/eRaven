//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.PositionUnitRepository;

public class PositionUnitRepository(IDbContextFactory<AppDbContext> dbFactory) : IPositionUnitRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <summary>
    /// Повернення всі посади
    /// </summary>
    /// <param name="ct"></param>
    /// <returns>IEnumerable PositionUnit<see cref="PositionUnit"/></returns>
    public async Task<IEnumerable<PositionUnit>> GetAllPositionUnitsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.PositionUnits
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Повертання вакантних посад з фільтром пошуку
    /// </summary>
    /// <param name="search"></param>
    /// <param name="take"></param>
    /// <param name="ct"></param>
    /// <returns>IReadOnlyList PositionUnitOptionDto<see cref="PositionUnit"/></returns>
    public async Task<IReadOnlyList<PositionUnitOptionDto>> GetVacantOptionsAsync(
       string? search,
       int take,
       CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.PositionUnits
            .AsNoTracking()
            .Where(x => x.IsActived)
            .Where(x => x.State == PositionUnitState.Vacant);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.Code.Contains(s) || x.ShortName.Contains(s) || x.FullName.Contains(s));
        }

        return await q
            .OrderBy(x => x.Number)
            .Take(take)
            .Select(x => new PositionUnitOptionDto(
                x.Id, x.Code, x.ShortName, x.FullName, x.Rank, x.Tarif
            ))
            .ToListAsync(ct);
    }

    public async Task<PositionUnitOptionDto?> GetOptionByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.PositionUnits
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new PositionUnitOptionDto(
                x.Id, x.Code, x.ShortName, x.FullName, x.Rank, x.Tarif
            ))
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Додавання посади
    /// </summary>
    /// <param name="positionUnit"></param>
    /// <param name="ct"></param>
    /// <returns>Task</returns>
    public async Task AddPositionUnitAsync(PositionUnit positionUnit, CancellationToken ct)
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
    public async Task DeActivatedPositionUnitAsync(Guid id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var position = await db.PositionUnits.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException($"PositionUnit '{id}' not found.");

        if (position.State != PositionUnitState.Vacant)
            throw new InvalidOperationException("Посада має зв'язок з персоналом.");

        position.IsActived = false;

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Перевірка на існування коду в базі
    /// </summary>
    /// <param name="code"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.PositionUnits.AsNoTracking().AnyAsync(x => x.Code == code, ct);
    }

    /// <summary>
    ///  Перевірка на активність номеру посади в базі
    /// </summary>
    /// <param name="number"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    public async Task<bool> ActiveNumberExistsAsync(int number, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.PositionUnits.AsNoTracking()
            .AnyAsync(x => x.Number == number && x.IsActived, ct);
    }
}
