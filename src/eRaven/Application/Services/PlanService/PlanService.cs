//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// Application: Services: Implementations
// План: етап 1 — записуємо планові дії і ОДРАЗУ виставляємо фактичні статуси
//-----------------------------------------------------------------------------

using eRaven.Application.Mappers;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PlanService;

public class PlanService(AppDbContext appDbContext) : IPlanService
{
    private readonly AppDbContext _appDbContext = appDbContext;

    public async Task<IEnumerable<Plan>> GetAllPlanAsync(CancellationToken ct = default)
    {
        // повертаємо доменні сутності (як у твоєму інтерфейсі)
        return await _appDbContext.Plans
             .Include(p => p.Order) // ← для OrderName
             .AsNoTracking()
             .OrderByDescending(p => p.RecordedUtc)
             .ToListAsync(ct);
    }

    public async Task<PlanViewModel?> GetByIdAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await _appDbContext.Plans
        .Include(p => p.Order) // ← для OrderName
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == planId, ct);

        return plan?.ToViewModel();
    }

    public async Task<PlanViewModel> CreateAsync(CreatePlanViewModel vm, CancellationToken ct = default)
    {
        var entity = vm.ToDomain();
        entity.Id = Guid.NewGuid();

        await _appDbContext.Plans.AddAsync(entity, ct);
        await _appDbContext.SaveChangesAsync(ct);

        return entity.ToViewModel();
    }

    public async Task<bool> DeleteAsync(Guid planId, CancellationToken ct = default)
    {
        // дозволяємо видалення тільки відкритого плану без наказу
        var plan = await _appDbContext.Plans
            .Include(p => p.PlanActions)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

        if (plan is null) return false;
        if (plan.State != PlanState.Open || plan.OrderId != null) return false;

        if (plan.PlanActions.Count > 0)
            _appDbContext.PlanActions.RemoveRange(plan.PlanActions);

        _appDbContext.Plans.Remove(plan);
        await _appDbContext.SaveChangesAsync(ct);
        return true;
    }
}