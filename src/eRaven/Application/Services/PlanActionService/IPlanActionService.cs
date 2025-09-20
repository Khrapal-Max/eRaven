//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPlanActionService
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanActionViewModels;
using eRaven.Domain.Models;

namespace eRaven.Application.Services.PlanActionService;

public interface IPlanActionService
{
    Task<IEnumerable<PlanAction?>> GetByIdAsync(Guid personId, CancellationToken ct = default);

    Task<PlanAction> CreateAsync(PlanAction planAction, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default); // тільки у стані PlanAction

    Task<PlanAction> ApproveAsync(ApprovePlanActionViewModel model, CancellationToken ct = default); // ставить Order + ApprovedOrder
}
