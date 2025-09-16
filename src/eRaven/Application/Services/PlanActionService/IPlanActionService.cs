//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPlanActionService
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;

namespace eRaven.Application.Services.PlanActionService;

public interface IPlanActionService
{
    /// <summary>
    /// Повернути активності плану
    /// </summary>
    /// <param name="planId"></param>
    /// <param name="ct"></param>
    /// <returns>IReadOnlyList PlanActionViewModel(<see cref="PlanActionViewModel"/>)</returns>
    Task<IReadOnlyList<PlanActionViewModel>> GetByPlanAsync(Guid planId, CancellationToken ct = default);

    /// <summary>
    /// Створити активність в плані
    /// </summary>
    /// <param name="vm"></param>
    /// <param name="ct"></param>
    /// <returns>PlanActionViewModel(<see cref="PlanActionViewModel"/>)</returns>
    Task<PlanActionViewModel> CreateAsync(CreatePlanActionViewModel vm, CancellationToken ct = default);

    /// <summary>
    /// Видалити активність в плані
    /// </summary>
    /// <param name="planActionId"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    Task<bool> DeleteAsync(Guid planActionId, CancellationToken ct = default);
}
