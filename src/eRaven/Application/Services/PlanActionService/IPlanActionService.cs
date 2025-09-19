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
    // ---------- читання ----------

    /// <summary>
    /// Отримати всі планові дії для особи, або порожній список, якщо не знайдено.
    /// </summary>
    /// <param name="personId"></param>
    /// <param name="onlyDraft"></param>
    /// <param name="ct"></param>
    /// <returns>IReadOnlyList PlanAction(<see cref="PlanAction"/>)</returns>
    Task<IReadOnlyList<PlanAction>> GetByPersonAsync(Guid personId, bool onlyDraft = false, CancellationToken ct = default);

    /// <summary>
    /// Отримати планову дію за Id, або null, якщо не знайдено.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns>PlanAction(<see cref="PlanAction"/>)</returns>
    Task<PlanAction?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Пошук планових дій за фільтром з пагінацією.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<PagedResult<PlanAction>> SearchAsync(string? search, CancellationToken ct = default);

    // ---------- створення / видалення ----------

    /// <summary>
    /// Створити планову дію (ActionState=PlanAction) з усіма валідаціями.
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="ct"></param>
    /// <returns>PlanAction(<see cref="PlanAction"/>)</returns>
    Task<PlanAction> AddActionAsync(CreatePlanActionDto dto, CancellationToken ct = default);

    /// <summary>
    /// Змінити існуючу чернетку (ActionState=PlanAction) з усіма валідаціями.
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<PlanAction> UpdateDraftAsync(UpdatePlanActionDto dto, CancellationToken ct = default);

    /// <summary>
    /// Видалити дію. Дозволено тільки для стану PlanAction.
    /// </summary>
    /// <param name="actionId"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    Task<bool> DeleteAsync(Guid actionId, CancellationToken ct = default);

    // ---------- затвердження (наказ) ----------

    /// <summary>
    /// Затвердити ОДНУ дію наказом: виставити ActionState=ApprovedOrder, 
    /// створити PersonStatus з OpenDate = EffectiveAtUtc, SourceDocumentType=\"PlanAction\", SourceDocumentId=action.Id.
    /// </summary>
    /// <param name="actionId"></param>
    /// <param name="options"></param>
    /// <param name="ct"></param>
    /// <returns>ApproveResult(<see cref="ApproveResult"/>)</returns>
    Task<ApproveResult> ApproveAsync(Guid actionId, ApproveOptions options, CancellationToken ct = default);

    /// <summary>
    /// Пакетне затвердження кількох дій наказом. 
    /// Всі гварди перевіряються і для перетинів між діями (до/після).
    /// </summary>
    /// <param name="actionIds"></param>
    /// <param name="options"></param>
    /// <param name="ct"></param>
    /// <returns>BatchApproveResult(<see cref="BatchApproveResult"/>)</returns>
    Task<BatchApproveResult> ApproveBatchAsync(IEnumerable<Guid> actionIds, ApproveOptions options, CancellationToken ct = default);

    // ---------- валідація без збереження ----------

    /// <summary>
    /// Перевірити, чи можна додати ТАКУ дію для особи (без запису).
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="ct"></param>
    /// <returns>IReadOnlyList string</returns>
    Task<IReadOnlyList<string>> ValidateNewActionAsync(CreatePlanActionDto dto, CancellationToken ct = default);

    /// <summary>
    /// Перевірити, чи можна затвердити перелік дій (без запису).
    /// </summary>
    /// <param name="actionIds"></param>
    /// <param name="ct"></param>
    /// <returns>IReadOnlyList string</returns>
    Task<IReadOnlyList<string>> ValidateApproveAsync(IEnumerable<Guid> actionIds, CancellationToken ct = default);
}
