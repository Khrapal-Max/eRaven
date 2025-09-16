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
    /// <summary>
    /// Повернути всі плани
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<IEnumerable<Plan>> GetAllPlanAsync(CancellationToken ct = default);

    /// <summary>
    /// Знайти план по Id (з діями). Повертає null, якщо не знайдено.
    /// </summary>
    /// <param name="planId"></param>
    /// <param name="ct"></param>
    /// <returns>PlanViewModel?(<see cref="PlanViewModel"/>)</returns>
    Task<PlanViewModel?> GetByIdAsync(Guid planId, CancellationToken ct = default);

    /// <summary>
    /// Створити план (порожній; дії додаються окремо іншим сервісом).
    /// </summary>
    /// <param name="createPlanViewModel"></param>
    /// <param name="ct"></param>
    /// <returns>PlanViewModel(<see cref="PlanViewModel"/>)</returns>
    Task<PlanViewModel> CreateAsync(CreatePlanViewModel createPlanViewModel, CancellationToken ct = default);

    /// <summary>
    /// Видалити план по Id. Повертає true, якщо видалено.
    /// </summary>
    /// <param name="planId"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    Task<bool> DeleteAsync(Guid planId, CancellationToken ct = default);
}
