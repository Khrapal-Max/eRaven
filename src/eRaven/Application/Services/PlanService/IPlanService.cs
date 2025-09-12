//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPlanService
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Models;

namespace eRaven.Application.Services.PlanService;

public interface IPlanService
{
    /// <summary>
    /// Повернути всі плани (можливо, з фільтром)
    /// </summary>
    /// <param name="planId"></param>
    /// <param name="ct"></param>
    /// <returns>IEnumerable Plan(<see cref="Plan"/>)</returns>
    Task<IEnumerable<Plan>> GetAllPlansAsync(CancellationToken ct = default);

    /// <summary>
    /// Повернути план за ідентифікатором
    /// </summary>
    /// <param name="planId"></param>
    /// <param name="ct"></param>
    /// <returns>Plan(<see cref="Plan"/>)</returns>
    Task<Plan?> GetByIdAsync(Guid planId, CancellationToken ct = default);

    /// <summary>
    /// Створити новий план
    /// </summary>
    /// <param name="planCreateViewModel"></param>
    /// <param name="ct"></param>
    /// <returns>CreatePlanCreateViewModel(<see cref="CreatePlanCreateViewModel"/>)</returns>
    Task<Plan> CreateAsync(CreatePlanViewModel planCreateViewModel, CancellationToken ct = default);

    /// <summary>
    /// Видалити план, якщо він відкритий
    /// </summary>
    /// <param name="planId"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    Task<bool> DeleteIfOpenAsync(Guid planId, CancellationToken ct = default);

    // ---------- НОВЕ: точкові операції ----------

    /// <summary>
    /// Дані для модала: всі люди + їх поточні статуси + стан у межах плану
    /// </summary>
    /// <param name="planId"></param>
    /// <param name="ct"></param>
    /// <returns>PlanRosterResponse(<see cref="PlanRosterResponse"/>)</returns>
    Task<PlanRosterResponse> GetPlanRosterAsync(Guid planId, CancellationToken ct = default);

    /// <summary>
    /// Додати один елемент з декількома людьми (без "Save changes" — одразу в БД)
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns>AddElementsResult(<see cref="AddElementsResult"/>)</returns>
    Task<AddElementsResult> AddElementsAsync(AddElementsRequest request, CancellationToken ct = default);

    /// <summary>
    /// Прибрати одного учасника з елемента (якщо порожній — видалити елемент)
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns>RemoveParticipantResult(<see cref="RemoveParticipantResult"/>)</returns>
    Task<RemoveParticipantResult> RemoveParticipantAsync(RemoveParticipantRequest request, CancellationToken ct = default);
}
