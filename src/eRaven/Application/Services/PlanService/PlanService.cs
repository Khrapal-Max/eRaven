//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// Application: Services: Implementations
// План: етап 1 — записуємо планові дії і ОДРАЗУ виставляємо фактичні статуси
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Models;

namespace eRaven.Application.Services.PlanService;

public sealed class PlanService : IPlanService
{
    public Task<IEnumerable<Plan>> GetAllPlanAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Plan?> GetPlanAsync(Guid planId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Plan> CreatePlanAsync(CreatePlanViewModel createPlanViewModel, string author, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ClosePlanAsync(Guid planId, string author, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }


    public Task<bool> DeletePlanAsync(Guid planId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<PlanAction> AddActioAsync(PlanActionViewModel vm, string author, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RemoveActionAsync(PlanActionViewModel vm, string author, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}