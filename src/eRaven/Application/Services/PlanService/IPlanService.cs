//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// IPlanService
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Models;

namespace eRaven.Application.Services.PlanService;

/// <summary>
/// Планування першого етапу: записуємо планові дії і ОДРАЗУ виставляємо фактичні статуси.
/// Накази/переривання під’їдуть наступним етапом.
/// </summary>
public interface IPlanService
{
    Task<IEnumerable<Plan>> GetAllPlanAsync(CancellationToken ct = default);
    Task<Plan?> GetPlanAsync(Guid planId, CancellationToken ct = default);

    Task<Plan> CreatePlanAsync(CreatePlanViewModel createPlanViewModel, string author, CancellationToken ct = default);
    Task<bool> ClosePlanAsync(Guid planId, string author, CancellationToken ct = default);
    Task<bool> DeletePlanAsync(Guid planId, CancellationToken ct = default);

    Task<PlanAction> AddActioAsync(PlanActionViewModel vm, string author, CancellationToken ct = default);
    Task<bool> RemoveActionAsync(PlanActionViewModel vm, string author, CancellationToken ct = default);
}
