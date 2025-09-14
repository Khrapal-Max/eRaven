//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanService
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PlanService;

public class PlanService(AppDbContext db) : IPlanService
{
    private readonly AppDbContext _db = db;

    public async Task<IEnumerable<Plan>> GetAllPlansAsync(CancellationToken ct = default)
        => await _db.Plans.AsNoTracking().OrderByDescending(p => p.RecordedUtc).ToListAsync(ct);

    public async Task<Plan?> GetByIdAsync(Guid planId, CancellationToken ct = default)
        => await _db.Plans.AsNoTracking()
            .Include(p => p.PlanElements.OrderBy(e => e.EventAtUtc))
                .ThenInclude(e => e.PlanParticipantSnapshot)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

    public async Task<Plan> CreateAsync(CreatePlanViewModel vm, CancellationToken ct = default)
    {
        var number = (vm.PlanNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(number)) throw new InvalidOperationException("№ плану обов’язковий.");
        if (await _db.Plans.AnyAsync(p => p.PlanNumber == number, ct))
            throw new InvalidOperationException("План з таким номером вже існує.");

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanNumber = number,
            State = vm.State,
            Author = "system",
            RecordedUtc = DateTime.UtcNow,
            PlanElements = []
        };
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);
        return plan;
    }

    public async Task<bool> CloseAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return false;
        if (plan.State == PlanState.Close) return true;

        plan.State = PlanState.Close;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteIfOpenAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return false;
        if (plan.State != PlanState.Open)
            throw new InvalidOperationException("План закритий — видалення заборонено.");

        _db.Plans.Remove(plan);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
