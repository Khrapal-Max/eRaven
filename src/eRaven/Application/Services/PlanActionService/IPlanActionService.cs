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
    /// <summary>
    /// Повертає всі PlanAction для конкретної особи
    /// </summary>
    /// <param name="personId"></param>
    /// <param name="ct"></param>
    /// <returns>IEnumerable PlanAction</returns>
    Task<IEnumerable<PlanAction?>> GetByIdAsync(Guid personId, int limit = default, CancellationToken ct = default);

    /// <summary>
    /// Створює PlanAction
    /// </summary>
    /// <param name="planAction"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<PlanAction> CreateAsync(PlanAction planAction, CancellationToken ct = default);

    /// <summary>
    /// Видаляє PlanAction
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default); // тільки у стані PlanAction

    /// <summary>
    /// Затверджує PlanAction
    /// </summary>
    /// <param name="model"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<PlanAction> ApproveAsync(ApprovePlanActionViewModel model, CancellationToken ct = default); // ставить Order + ApprovedOrder
}
