//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusKindService
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.StatusKindViewModels;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.StatusKindService;

public sealed class StatusKindService(AppDbContext db) : IStatusKindService
{
    private readonly AppDbContext _db = db;

    public async Task<IReadOnlyList<StatusKind>> GetAllAsync(bool includeInactive = true, CancellationToken ct = default)
    {
        var query = _db.StatusKinds.AsNoTracking();
        if (!includeInactive) query = query.Where(k => k.IsActive);

        return await query.OrderBy(k => k.Order)
                      .ThenBy(k => k.Name)
                      .ToListAsync(ct);
    }

    public Task<StatusKind?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.StatusKinds.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id, ct);

    public async Task<StatusKind> CreateAsync(CreateKindViewModel newKindViewModel, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newKindViewModel.Name)) throw new ArgumentException("Name required", nameof(newKindViewModel));
        if (string.IsNullOrWhiteSpace(newKindViewModel.Code)) throw new ArgumentException("Code required", nameof(newKindViewModel));

        var entity = new StatusKind
        {
            Name = newKindViewModel.Name.Trim(),
            Code = newKindViewModel.Code.Trim(),
            Order = newKindViewModel.Order,
            IsActive = newKindViewModel.IsActive,
            Author = "ui",
            Modified = DateTime.UtcNow
        };

        _db.StatusKinds.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var status = await _db.StatusKinds.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (status is null) return false;
        if (status.IsActive == isActive) return true;

        status.IsActive = isActive;
        status.Modified = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateOrderAsync(int id, int newOrder, CancellationToken ct = default)
    {
        var status = await _db.StatusKinds.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (status is null) return false;

        status.Order = newOrder;
        status.Modified = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return Task.FromResult(false);

        var q = _db.StatusKinds.AsNoTracking()
            .Where(p => p.Name == name);

        return q.AnyAsync(ct);
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return Task.FromResult(false);

        var q = _db.StatusKinds.AsNoTracking()
            .Where(p => p.Code == code);

        return q.AnyAsync(ct);
    }
}
