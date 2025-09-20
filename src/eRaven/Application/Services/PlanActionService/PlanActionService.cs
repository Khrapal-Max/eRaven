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

public sealed class PlanActionService(AppDbContext db) : IPlanActionService
{
    private readonly AppDbContext _db = db;

    /// <summary>
    /// Повертає всі PlanAction для конкретної особи
    /// </summary>
    /// <param name="personId"></param>
    /// <param name="ct"></param>
    /// <returns>IEnumerable PlanAction?(<see cref="PlanAction"/>)</returns>
    public async Task<IEnumerable<PlanAction?>> GetByIdAsync(Guid personId, CancellationToken ct = default)
    {
        var actions = await _db.PlanActions
            .AsNoTracking()
            .Where(x => x.PersonId == personId)
            .ToListAsync(ct);

        return actions ?? [];
    }

    /// <summary>
    /// Створює PlanAction
    /// </summary>
    /// <param name="planAction"></param>
    /// <param name="ct"></param>
    /// <returns>PlanAction(<see cref="PlanAction"/>)</returns>
    public async Task<PlanAction> CreateAsync(PlanAction planAction, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(planAction, nameof(planAction));

        var action = new PlanAction
        {
            Id = planAction.Id != Guid.Empty ? planAction.Id : Guid.NewGuid(),
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

        _db.PlanActions.Add(action);          // графу більше немає — лише сам PlanAction
        await _db.SaveChangesAsync(ct);

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
        var action = _db.PlanActions
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == id);

        if (action is null || action.ActionState != ActionState.PlanAction)
            return false;

        _db.Remove(action);
        await _db.SaveChangesAsync(ct);

        return true;
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
        var action = _db.PlanActions
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == model.Id);

        if (action is null || action.ActionState != ActionState.PlanAction)
            throw new InvalidOperationException("PlanAction not found or not in PlanAction state.");

        action.ActionState = ActionState.ApprovedOrder;
        action.Order = model.Order;

        _db.PlanActions.Update(action);
        await _db.SaveChangesAsync(ct);

        return action;
    }
}