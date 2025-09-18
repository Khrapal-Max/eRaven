//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPlanActionService
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Models;

namespace eRaven.Application.Services.PlanActionService;

public interface IPlanActionService
{
    Task<List<PlanAction>> GetAllByPlanIdAsync(Guid planId, CancellationToken ct = default);

    Task<PlanAction> CreateAsync(Guid planId, CreatePlanActionModel model, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid actionId, CancellationToken ct = default);

    Task<bool> AssignTripIdAsync(Guid dispatchActionId, Guid returnActionId, CancellationToken ct = default);
}
