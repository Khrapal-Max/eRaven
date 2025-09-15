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
    // ---- Read ----
    Task<IReadOnlyList<Plan>> GetAllPlansAsync(CancellationToken ct = default);
    Task<Plan?> GetPlanAsync(Guid planId, CancellationToken ct = default);
    Task<IReadOnlyList<PlanParticipant>> GetPlanParticipantsAsync(Guid planId, CancellationToken ct = default);
    Task<IReadOnlyList<PlanParticipantAction>> GetPlanActionsAsync(Guid planId, CancellationToken ct = default);

    // ---- Commands (Stage 1) ----
    Task<Plan> EnsurePlanAsync(CreatePlanViewModel vm, string author, CancellationToken ct = default);
    Task<PlanParticipant> EnsureParticipantAsync(string planNumber, Guid personId, string author, CancellationToken ct = default);
    Task<PlanParticipantAction> AddActionAndApplyStatusAsync(PlanActionViewModel vm, string author, CancellationToken ct = default);
    Task ApplyBatchAsync(PlanBatchViewModel vm, string author, CancellationToken ct = default);

    // ---- Plan lifecycle ----
    Task<bool> ClosePlanAsync(Guid planId, string author, CancellationToken ct = default);
    Task<bool> DeletePlanAsync(Guid planId, CancellationToken ct = default);
}
