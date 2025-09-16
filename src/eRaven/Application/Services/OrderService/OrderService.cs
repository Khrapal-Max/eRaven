//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// OrderService
//-----------------------------------------------------------------------------

using eRaven.Application.Mappers;
using eRaven.Application.ViewModels.OrderViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.OrderService;

public class OrderService(AppDbContext appDbContext) : IOrderService
{
    private readonly AppDbContext _appDbContext = appDbContext;

    public async Task<IEnumerable<OrderViewModel>> GetAllOrderAsync(CancellationToken ct = default)
    {
        var items = await _appDbContext.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.RecordedUtc)
            .ToListAsync(ct);

        return items.ToViewModels();
    }

    public async Task<OrderDetailsViewModel?> GetByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _appDbContext.Orders
            .Include(o => o.Plans)
            .Include(o => o.Actions)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        return order?.ToDetailsViewModel();
    }

    public async Task<ExecutedPublishDailyOrderViewModel> CreateAsync(
        CreatePublishDailyOrderViewModel request,
        CancellationToken ct = default)
    {
        await using var tx = await _appDbContext.Database.BeginTransactionAsync(ct);

        // 1) створюємо наказ
        var order = request.ToDomain();
        order.Id = Guid.NewGuid();
        await _appDbContext.Orders.AddAsync(order, ct);

        // 2) тягнемо відкриті плани без наказу
        var planIds = request.PlanIds.Distinct().ToArray();

        var plans = await _appDbContext.Plans
            .Include(p => p.PlanActions)
            .Where(p => planIds.Contains(p.Id) && p.State == PlanState.Open && p.OrderId == null)
            .ToListAsync(ct);

        // якщо жодного — все одно створювати наказ чи кидати? тут — кидаємо
        if (plans.Count == 0)
            throw new InvalidOperationException("Немає відкритих планів для закриття.");

        // 3) авто-Return там, де остання дія в межах плану — Dispatch
        if (request.AutoReturnForOpenDispatch)
        {
            foreach (var plan in plans)
            {
                // останній запис по кожній особі в межах плану
                var lastByPerson = plan.PlanActions
                    .GroupBy(a => a.PersonId)
                    .Select(g => g.OrderByDescending(x => x.EventAtUtc).First());

                // беремо тільки Dispatch
                var toReturn = lastByPerson.Where(a => a.ActionType == PlanActionType.Dispatch).ToList();

                if (toReturn.Count == 0) continue;

                // підвантажимо людей для можливого оновлення snapshot (не обов’язково)
                var personIds = toReturn.Select(x => x.PersonId).Distinct().ToArray();
                var people = await _appDbContext.Persons
                    .Where(p => personIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, ct);

                foreach (var last in toReturn)
                {
                    var p = people.TryGetValue(last.PersonId, out var pers) ? pers : last.Person;

                    var autoReturn = new PlanAction
                    {
                        Id = Guid.NewGuid(),
                        PlanId = plan.Id,
                        Plan = plan,
                        PersonId = p.Id,
                        Person = p,
                        ActionType = PlanActionType.Return,
                        EventAtUtc = order.EffectiveMomentUtc, // час наказу
                        Location = last.Location,
                        GroupName = last.GroupName,
                        CrewName = last.CrewName,

                        // snapshot: можна взяти з останнього або оновити з Person
                        Rnokpp = p.Rnokpp,
                        FullName = p.FullName,
                        RankName = p.Rank,
                        PositionName = p.PositionUnit?.FullName ?? last.PositionName,
                        BZVP = p.BZVP,
                        Weapon = p.Weapon ?? last.Weapon,
                        Callsign = p.Callsign ?? last.Callsign,
                        StatusKindOnDate = "Повернувся"
                    };

                    plan.PlanActions.Add(autoReturn);
                    await _appDbContext.PlanActions.AddAsync(autoReturn, ct);
                }
            }
        }

        // 4) підтверджуємо дії, які увімкнув UI (або всі, якщо Include=null)
        var includeSet = request.IncludePlanActionIds is null
            ? null
            : new HashSet<Guid>(request.IncludePlanActionIds);

        var confirmed = new List<OrderAction>();

        foreach (var plan in plans)
        {
            var actionsToConfirm = plan.PlanActions
                .Where(a => includeSet is null || includeSet.Contains(a.Id))
                .OrderBy(a => a.EventAtUtc)
                .ToList();

            foreach (var a in actionsToConfirm)
            {
                var oa = new OrderAction
                {
                    Id = Guid.NewGuid(),
                    Order = order,
                    OrderId = order.Id,
                    PlanId = plan.Id,
                    PlanActionId = a.Id,
                    PersonId = a.PersonId,
                    Person = a.Person,

                    ActionType = a.ActionType,
                    EventAtUtc = a.EventAtUtc,
                    Location = a.Location,
                    GroupName = a.GroupName,
                    CrewName = a.CrewName,

                    // snapshot
                    Rnokpp = a.Rnokpp,
                    FullName = a.FullName,
                    RankName = a.RankName,
                    PositionName = a.PositionName,
                    BZVP = a.BZVP,
                    Weapon = a.Weapon,
                    Callsign = a.Callsign,
                    StatusKindOnDate = a.StatusKindOnDate
                };

                confirmed.Add(oa);
            }

            // 5) закриваємо план та прив’язуємо до наказу
            plan.State = PlanState.Close;
            plan.Order = order;
            plan.OrderId = order.Id;
        }

        await _appDbContext.OrderActions.AddRangeAsync(confirmed, ct);
        await _appDbContext.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // 6) готуємо результат
        return order.ToExecutedViewModel(plans, confirmed);
    }

    public async Task<bool> DeleteAsync(Guid orderId, CancellationToken ct = default)
    {
        // політика: не видаляємо наказ із підв’язаними планами або діями
        var order = await _appDbContext.Orders
            .Include(o => o.Plans)
            .Include(o => o.Actions)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null) return false;
        if (order.Plans.Count != 0 || order.Actions.Count != 0) return false;

        _appDbContext.Orders.Remove(order);
        await _appDbContext.SaveChangesAsync(ct);
        return true;
    }
}
