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

public class PositionService(IDbContextFactory<AppDbContext> dbf) : IPositionService
{
    private readonly IDbContextFactory<AppDbContext> _dbf = dbf;

    public async Task<IReadOnlyList<PositionUnit>> GetPositionsAsync(bool onlyActive = true, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        var q = db.PositionUnits
            .AsNoTracking()
            .Include(x => x.CurrentPerson)
            .Where(p => p.IsActived);

        return await q.ToListAsync(ct);
    }

    public async Task<PositionUnit?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        return await db.PositionUnits.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<PositionUnit> CreatePositionAsync(PositionUnit positionUnit, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        if (string.IsNullOrWhiteSpace(positionUnit.ShortName))
            throw new ArgumentException("Назва обов'язкова.", nameof(positionUnit));

        // Нормалізація
        positionUnit.Code = string.IsNullOrWhiteSpace(positionUnit.Code) ? null : positionUnit.Code.Trim();

        // Якщо створюємо активну і є код — перевіряємо дубль серед активних
        if (positionUnit.IsActived && !string.IsNullOrEmpty(positionUnit.Code))
        {
            var exists = await ExistsActiveWithCodeAsync(positionUnit.Code!, excludeId: null, ct);
            if (exists)
                throw new InvalidOperationException("Активна посада з таким кодом вже існує.");
        }

        var entry = await db.PositionUnits.AddAsync(positionUnit, ct);
        await db.SaveChangesAsync(ct);
        return entry.Entity;
    }

    public async Task<bool> SetActiveStateAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        var pos = await db.PositionUnits.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pos is null) return false;

        if (pos.IsActived == isActive)
            return true;

        // Нормалізація коду
        pos.Code = string.IsNullOrWhiteSpace(pos.Code) ? null : pos.Code.Trim();

        if (!isActive)
        {
            // Забороняємо деактивацію, якщо посада закріплена
            var occupied = await db.Persons.AsNoTracking()
                .AnyAsync(p => p.PositionUnitId == id, ct);

            if (occupied)
                throw new InvalidOperationException("Неможливо деактивувати посаду, поки вона закріплена за людиною.");

            pos.IsActived = false;
        }
        else
        {
            // Активація: забороняємо дубль коду серед активних (окрім самої себе)
            if (!string.IsNullOrEmpty(pos.Code))
            {
                var duplicateExists = await ExistsActiveWithCodeAsync(pos.Code!, excludeId: id, ct);
                if (duplicateExists)
                    throw new InvalidOperationException("Активна посада з таким кодом вже існує.");
            }

            pos.IsActived = true;
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> CodeExistsActiveAsync(string code, CancellationToken ct = default)
        => ExistsActiveWithCodeAsync(code?.Trim() ?? string.Empty, excludeId: null, ct);

    // ---- Private helper ----
    private async Task<bool> ExistsActiveWithCodeAsync(string code, Guid? excludeId, CancellationToken ct)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        if (string.IsNullOrWhiteSpace(code)) return false;

        var q = db.PositionUnits.AsNoTracking()
            .Where(p => p.IsActived && p.Code != null && p.Code == code);

        if (excludeId is not null)
            q = q.Where(p => p.Id != excludeId.Value);

        return await q.AnyAsync(ct);
    }
}
