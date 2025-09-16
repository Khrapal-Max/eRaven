//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActionService
//-----------------------------------------------------------------------------

using eRaven.Application.Mappers;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PlanActionService;

public sealed class PlanActionService(AppDbContext db) : IPlanActionService
{
    private readonly AppDbContext _db = db;

    public async Task<IReadOnlyList<PlanActionViewModel>> GetByPlanAsync(Guid planId, CancellationToken ct = default)
    {
        // достатньо snapshot-полів; навігації не потрібні
        var actions = await _db.PlanActions
            .AsNoTracking()
            .Where(a => a.PlanId == planId)
            .OrderBy(a => a.EventAtUtc)
            .ToListAsync(ct);

        return actions.ToViewModels();
    }

    public async Task<PlanActionViewModel> CreateAsync(CreatePlanActionViewModel vm, CancellationToken ct = default)
    {
        // 1) Перевіряємо план
        var plan = await _db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == vm.PlanId, ct);

        if (plan is null)
            throw new InvalidOperationException("План не знайдено.");

        if (plan.State != PlanState.Open || plan.OrderId is not null)
            throw new InvalidOperationException("Додавати дії можна лише до відкритого плану без наказу.");

        // 2) Перевіряємо особу
        var person = await _db.Persons
            .Include(p => p.PositionUnit)
            .Include(p => p.StatusKind)
            .FirstOrDefaultAsync(p => p.Id == vm.PersonId, ct);

        if (person is null)
            throw new InvalidOperationException("Особа не знайдена.");

        // 3) Нормалізуємо час у UTC
        var eventAtUtc = vm.EventAtUtc.Kind == DateTimeKind.Utc
            ? vm.EventAtUtc
            : DateTime.SpecifyKind(vm.EventAtUtc, DateTimeKind.Utc);

        // 4) Створюємо дію зі знімком Person
        var action = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            PersonId = person.Id,

            ActionType = vm.ActionType,
            EventAtUtc = eventAtUtc,
            Location = vm.Location,
            GroupName = vm.GroupName,
            CrewName = vm.CrewName,

            Rnokpp = person.Rnokpp,
            FullName = person.FullName,
            RankName = person.Rank,
            PositionName = person.PositionUnit?.FullName ?? string.Empty,
            BZVP = person.BZVP,
            Weapon = person.Weapon ?? string.Empty,
            Callsign = person.Callsign ?? string.Empty,
            StatusKindOnDate = person.StatusKind?.Name ?? string.Empty
        };

        await _db.PlanActions.AddAsync(action, ct);
        await _db.SaveChangesAsync(ct);

        return action.ToViewModel();
    }

    public async Task<bool> DeleteAsync(Guid planActionId, CancellationToken ct = default)
    {
        // Потрібно перевірити стан плану: якщо план закритий або вже прив’язаний до наказу — заборонити
        var action = await _db.PlanActions
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Id == planActionId, ct);

        if (action is null) return false;

        if (action.Plan.State != PlanState.Open || action.Plan.OrderId is not null)
            return false;

        _db.PlanActions.Remove(action);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
