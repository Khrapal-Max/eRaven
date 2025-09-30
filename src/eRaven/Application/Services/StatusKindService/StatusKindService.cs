//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusKindService
//-----------------------------------------------------------------------------

using eRaven.Application.Services.Clock;
using eRaven.Application.ViewModels.StatusKindViewModels;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.StatusKindService;

public sealed class StatusKindService(IDbContextFactory<AppDbContext> dbf, IClock clock) : IStatusKindService
{
    private readonly IDbContextFactory<AppDbContext> _dbf = dbf;
    private readonly IClock _clock = clock;

    public async Task<IReadOnlyList<StatusKind>> GetAllAsync(bool includeInactive = true, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        var query = db.StatusKinds.AsNoTracking();
        if (!includeInactive) query = query.Where(k => k.IsActive);

        return await query.OrderBy(k => k.Order)
                      .ThenBy(k => k.Name)
                      .ToListAsync(ct);
    }

    public async Task<StatusKind?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        return await db.StatusKinds.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id, ct);
    }

    public async Task<StatusKind> CreateAsync(CreateKindViewModel newKindViewModel, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        if (string.IsNullOrWhiteSpace(newKindViewModel.Name)) throw new ArgumentException("Name required", nameof(newKindViewModel));
        if (string.IsNullOrWhiteSpace(newKindViewModel.Code)) throw new ArgumentException("Code required", nameof(newKindViewModel));

        var entity = new StatusKind
        {
            Name = newKindViewModel.Name.Trim(),
            Code = newKindViewModel.Code.Trim(),
            Order = newKindViewModel.Order,
            IsActive = newKindViewModel.IsActive,
            Author = "ui",
            Modified = _clock.UtcNow
        };

        db.StatusKinds.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        var status = await db.StatusKinds.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (status is null) return false;
        if (status.IsActive == isActive) return true;

        status.IsActive = isActive;
        status.Modified = _clock.UtcNow;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateOrderAsync(int id, int newOrder, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        var status = await db.StatusKinds.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (status is null) return false;

        status.Order = newOrder;
        status.Modified = _clock.UtcNow;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> NameExistsAsync(string name, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        if (string.IsNullOrWhiteSpace(name)) return false;

        var q = db.StatusKinds.AsNoTracking()
            .Where(p => p.Name == name);

        return await q.AnyAsync(ct);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        if (string.IsNullOrWhiteSpace(code)) return false;

        var q = db.StatusKinds.AsNoTracking()
            .Where(p => p.Code == code);

        return await q.AnyAsync(ct);
    }
}
