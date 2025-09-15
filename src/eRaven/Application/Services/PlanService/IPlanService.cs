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
    /// Створює план або повертає існуючий за унікальним номером.
    /// PlanNumber нормалізується (Trim). State ігнорується (на першому етапі завжди Open).
    /// </summary>
    Task<Plan> EnsurePlanAsync(CreatePlanViewModel vm, string author, CancellationToken ct = default);

    /// <summary>
    /// Додає учасника для плану (або повертає існуючого) з обов’язковим снапшотом атрибутів Person:
    /// FullName, RankName, PositionName, UnitName (відповідає бізнес-вимогам).
    /// </summary>
    Task<PlanParticipant> EnsureParticipantAsync(string planNumber, Guid personId, string author, CancellationToken ct = default);

    /// <summary>
    /// Додає одну дію (Dispatch/Return) та ОДРАЗУ виставляє PersonStatus згідно з PlanServiceOptions.
    /// Валідатор блокує: дубльований Dispatch без Return, Return без Dispatch, некоректну хронологію.
    /// Поля Location/GroupName/CrewName мають бути заповнені (trim + not blank).
    /// </summary>
    Task<PlanParticipantAction> AddActionAndApplyStatusAsync(PlanActionViewModel vm, string author, CancellationToken ct = default);

    /// <summary>
    /// Пакетний режим: приймає багато дій у межах одного плану, гарантує транзакційність.
    /// На кожній дії виставляється PersonStatus. Порядок всередині кожної особи — за EventAtUtc.
    /// </summary>
    Task ApplyBatchAsync(PlanBatchViewModel vm, string author, CancellationToken ct = default);
}
