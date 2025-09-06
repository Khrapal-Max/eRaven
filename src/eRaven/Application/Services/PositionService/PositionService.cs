//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionService
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PositionService;

public class PositionService(AppDbContext appDbContext) : IPositionService
{
    private readonly AppDbContext _appDbContext = appDbContext;

    // 1) Лістинг (опційно — тільки активні), без трекінгу, відсортований
    public async Task<IReadOnlyList<PositionUnit>> GetPositionsAsync(
        bool onlyActive = true,
        CancellationToken ct = default)
    {
        var positions = _appDbContext.Positions.AsNoTracking();

        if (onlyActive)
            positions = positions.Where(p => p.IsActived);

        return await positions.ToListAsync(ct);
    }

    // 2) Отримання однієї
    public Task<PositionUnit?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _appDbContext.Positions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    // 3) Створення
    public async Task<PositionUnit> CreatePositionAsync(PositionUnit positionUnit, CancellationToken ct = default)
    {
        // міні-валидація
        if (string.IsNullOrWhiteSpace(positionUnit.ShortName))
            throw new ArgumentException("Назва обов'язкова.", nameof(positionUnit));

        positionUnit.IsActived = true; // за замовчуванням активуємо

        var entry = await _appDbContext.Positions.AddAsync(positionUnit, ct);

        await _appDbContext.SaveChangesAsync(ct);

        return entry.Entity;
    }

    // 4) Деактивація з перевіркою, що посада не зайнята
    public async Task<bool> SetActiveStateAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var pos = await _appDbContext.Positions.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pos is null) return false;

        if (pos.IsActived == isActive)
            return true; // вже у потрібному стані

        if (!isActive)
        {
            // при деактивації перевіряємо, що посада не зайнята
            var occupied = await _appDbContext.Persons
                .AsNoTracking()
                .AnyAsync(p => p.PositionUnitId == id, ct);

            if (occupied)
                throw new InvalidOperationException("Неможливо деактивувати посаду, поки вона закріплена за людиною.");
        }

        pos.IsActived = isActive;
        await _appDbContext.SaveChangesAsync(ct);
        return true;
    }
}
