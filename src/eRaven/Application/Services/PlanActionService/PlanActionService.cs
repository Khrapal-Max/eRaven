//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanActionService
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanActionViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PlanActionService;

public sealed class PlanActionService(IDbContextFactory<AppDbContext> dbf) : IPlanActionService
{
    private readonly IDbContextFactory<AppDbContext> _dbf = dbf;

    /// <summary>
    /// Повертає всі PlanAction для конкретної особи
    /// </summary>
    /// <param name="personId"></param>
    /// <param name="ct"></param>
    /// <returns>IEnumerable PlanAction?(<see cref="PlanAction"/>)</returns>
    public async Task<IEnumerable<PlanAction?>> GetByIdAsync(Guid personId, int limit = 150, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        if (limit <= 0) limit = 150;

        var actions = await db.PlanActions
            .AsNoTracking()
            .Where(x => x.PersonId == personId)
            .OrderByDescending(x => x.EffectiveAtUtc)
            .Take(limit)
            .ToListAsync(ct);

        return actions;
    }

    /// <summary>
    /// Створює PlanAction
    /// </summary>
    /// <param name="planAction"></param>
    /// <param name="ct"></param>
    /// <returns>PlanAction(<see cref="PlanAction"/>)</returns>
    public async Task<PlanAction> CreateAsync(PlanAction planAction, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        ArgumentNullException.ThrowIfNull(planAction, nameof(planAction));

        var action = new PlanAction
        {
            Id = planAction.Id,
            PersonId = planAction.PersonId,
            PlanActionName = planAction.PlanActionName,
            EffectiveAtUtc = planAction.EffectiveAtUtc,
            ToStatusKindId = null,
            Order = null,
            ActionState = planAction.ActionState,     // або примусово ActionState.PlanAction
            MoveType = planAction.MoveType,
            Location = planAction.Location?.Trim() ?? string.Empty,
            GroupName = planAction.GroupName?.Trim() ?? string.Empty,
            CrewName = planAction.CrewName?.Trim() ?? string.Empty,
            Note = planAction.Note?.Trim() ?? string.Empty,

            // snapshot:
            Rnokpp = planAction.Rnokpp,
            FullName = planAction.FullName,
            RankName = planAction.RankName,
            PositionName = planAction.PositionName,
            BZVP = planAction.BZVP,
            Weapon = planAction.Weapon,
            Callsign = planAction.Callsign,
            StatusKindOnDate = planAction.StatusKindOnDate
        };

        db.PlanActions.Add(action);          // графу більше немає — лише сам PlanAction
        await db.SaveChangesAsync(ct);

        return action;
    }

    /// <summary>
    /// Видаляє PlanAction, якщо вона в стані PlanAction
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        var affected = await db.PlanActions
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(ct);

        return affected > 0;
    }

    /// <summary>
    /// Закріплює PlanAction, ставить Order та змінює стан на ApprovedOrder
    /// </summary>
    /// <param name="model"></param>
    /// <param name="ct"></param>
    /// <returns>PlanAction(<see cref="PlanAction"/>)</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<PlanAction> ApproveAsync(ApprovePlanActionViewModel model, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        var updated = await db.PlanActions
            .Where(a => a.Id == model.Id && a.ActionState == ActionState.PlanAction)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.ActionState, ActionState.ApprovedOrder)
                .SetProperty(a => a.Order, model.Order), ct);

        if (updated == 0)
            throw new InvalidOperationException("PlanAction not found or not in PlanAction state.");

        return await db.PlanActions.AsNoTracking().FirstAsync(a => a.Id == model.Id, ct);
    }
}