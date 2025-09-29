//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanActionService
//-----------------------------------------------------------------------------

using eRaven.Application.Projector;
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
    /// Повертає планові дії на проміжок часу [fromUtc, toUtc),
    /// </summary>
    /// <param name="atUtc"></param>
    /// <param name="ct"></param>
    /// <returns>IReadOnlyList PlanAction(<see cref="PlanAction"/>)</returns>
    public async Task<IReadOnlyList<PlanAction>> GetActiveDispatchOnDateAsync(
      DateTime atUtc,
      CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        // 1) Беремо всі дії до моменту (включно) в актуальних станах
        var items = await db.PlanActions
            .AsNoTracking()
            .Where(a => a.EffectiveAtUtc <= atUtc)
            .Where(a => a.ActionState == ActionState.PlanAction
                     || a.ActionState == ActionState.ApprovedOrder)
            // спочатку відсортуємо так, щоб "остання/найпріоритетніша" була першою в групі
            .OrderByDescending(a => a.EffectiveAtUtc)
            .ThenByDescending(a => a.ActionState == ActionState.ApprovedOrder) // Approved > PlanAction
            .ThenByDescending(a => a.Id) // стабілізатор
            .ToListAsync(ct);

        if (items.Count == 0) return [];

        // 2) По особі беремо останню дію; включаємо лише Dispatch
        var lastByPerson = items
            .GroupBy(a => a.PersonId)
            .Select(g => g.First())                           // завдяки сортуванню вище — це “остання” на дату
            .Where(a => a.MoveType == MoveType.Dispatch)     // у відрядженні на дату
            .ToList();

        // 3) Фінальне впорядкування для звіту
        var ordered = lastByPerson
            .OrderBy(a => a.Location ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.GroupName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.CrewName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.FullName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ordered.AsReadOnly();
    }

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

        var person = await db.Persons
            .AsNoTracking()
            .Include(p => p.PlanActions)
            .FirstOrDefaultAsync(p => p.Id == planAction.PersonId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена.");

        var location = planAction.Location?.Trim() ?? string.Empty;
        var group = planAction.GroupName?.Trim() ?? string.Empty;
        var crew = planAction.CrewName?.Trim() ?? string.Empty;
        var note = planAction.Note?.Trim() ?? string.Empty;

        var snapshot = PlanAction.Create(
            planAction.PersonId,
            planAction.PlanActionName,
            planAction.EffectiveAtUtc,
            null,
            null,
            planAction.ActionState,
            planAction.MoveType,
            location,
            group,
            crew,
            note,
            planAction.Rnokpp,
            planAction.FullName,
            planAction.RankName,
            planAction.PositionName,
            planAction.BZVP,
            planAction.Weapon,
            planAction.Callsign,
            planAction.StatusKindOnDate);

        if (planAction.Id != Guid.Empty)
        {
            snapshot.Id = planAction.Id;
        }

        var created = person.AddPlanAction(snapshot);
        created.Person = person;

        await PersonAggregateProjector.ProjectAsync(db, person, ct);
        await db.SaveChangesAsync(ct);

        return created;
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