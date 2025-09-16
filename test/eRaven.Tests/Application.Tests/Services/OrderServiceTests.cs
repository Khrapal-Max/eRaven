//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// OrderServiceTests
//-----------------------------------------------------------------------------

using eRaven.Application.Services.OrderService;
using eRaven.Application.ViewModels.OrderViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Tests.Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Tests.Services;

public class OrderServiceTests
{
    // --------------- helpers ----------------
    private static Person NewPerson(string rnokpp, string last, string first) => new()
    {
        Id = Guid.NewGuid(),
        Rnokpp = rnokpp,
        Rank = "Солдат",
        LastName = last,
        FirstName = first,
        BZVP = "Y",
        Weapon = "",
        Callsign = ""
    };

    private static PlanAction NewPlanAction(Plan plan, Person person,
        PlanActionType type, DateTime whenUtc, string status = "Статус")
        => new()
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Plan = plan,
            PersonId = person.Id,
            Person = person,
            ActionType = type,
            EventAtUtc = DateTime.SpecifyKind(whenUtc, DateTimeKind.Utc),
            Location = "Loc",
            GroupName = "Grp",
            CrewName = "Crew",
            Rnokpp = person.Rnokpp,
            FullName = $"{person.LastName} {person.FirstName}",
            RankName = person.Rank,
            PositionName = "Командир відділення",
            BZVP = person.BZVP,
            Weapon = person.Weapon ?? "",
            Callsign = person.Callsign ?? "",
            StatusKindOnDate = status
        };

    // --------------- tests ------------------

    [Fact]
    public async Task GetAllOrderAsync_Returns_OrderedByRecordedUtc_Desc()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;

        var older = new Order { Id = Guid.NewGuid(), Name = "A", EffectiveMomentUtc = DateTime.UtcNow.AddHours(-3), RecordedUtc = DateTime.UtcNow.AddHours(-3) };
        var mid = new Order { Id = Guid.NewGuid(), Name = "B", EffectiveMomentUtc = DateTime.UtcNow.AddHours(-2), RecordedUtc = DateTime.UtcNow.AddHours(-2) };
        var newer = new Order { Id = Guid.NewGuid(), Name = "C", EffectiveMomentUtc = DateTime.UtcNow.AddHours(-1), RecordedUtc = DateTime.UtcNow.AddHours(-1) };
        await db.Orders.AddRangeAsync(older, mid, newer);
        await db.SaveChangesAsync();

        var svc = new OrderService(db);

        // Act
        var list = (await svc.GetAllOrderAsync()).ToList();

        // Assert
        Assert.Equal(3, list.Count);
        Assert.Equal("C", list[0].Name);
        Assert.Equal("B", list[1].Name);
        Assert.Equal("A", list[2].Name);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Details_WithPlans_AndActions()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;

        var order = new Order { Id = Guid.NewGuid(), Name = "НК-1", EffectiveMomentUtc = DateTime.UtcNow };
        var p1 = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-1", State = PlanState.Close, Order = order, OrderId = order.Id };
        var p2 = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-2", State = PlanState.Close, Order = order, OrderId = order.Id };
        var person = NewPerson("1111111111", "Івашенко", "Павло");
        await db.Orders.AddAsync(order);
        await db.Plans.AddRangeAsync(p1, p2);
        await db.Persons.AddAsync(person);
        await db.SaveChangesAsync();

        var pa = NewPlanAction(p1, person, PlanActionType.Dispatch, DateTime.UtcNow.AddMinutes(-30));
        await db.PlanActions.AddAsync(pa);
        await db.SaveChangesAsync();

        var oa = new OrderAction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            PlanId = p1.Id,
            PlanActionId = pa.Id,
            PersonId = person.Id,
            Person = person,
            ActionType = pa.ActionType,
            EventAtUtc = pa.EventAtUtc,
            Location = pa.Location,
            GroupName = pa.GroupName,
            CrewName = pa.CrewName,
            Rnokpp = pa.Rnokpp,
            FullName = pa.FullName,
            RankName = pa.RankName,
            PositionName = pa.PositionName,
            BZVP = pa.BZVP,
            Weapon = pa.Weapon,
            Callsign = pa.Callsign,
            StatusKindOnDate = pa.StatusKindOnDate
        };
        await db.OrderActions.AddAsync(oa);
        await db.SaveChangesAsync();

        var svc = new OrderService(db);

        // Act
        var details = await svc.GetByIdAsync(order.Id);

        // Assert
        Assert.NotNull(details);
        Assert.Equal(order.Id, details!.Order.Id);
        Assert.Equal(2, details.PlanIds.Count);
        Assert.Single(details.Actions);
        Assert.Equal(oa.Id, details.Actions[0].Id);
    }

    [Fact]
    public async Task CreateAsync_CreatesOrder_ClosesPlans_ConfirmsAll_IncludingAutoReturn()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;
        var svc = new OrderService(db);

        // persons
        var pA = NewPerson("1111111111", "Каменяр", "А");
        var pB = NewPerson("2222222222", "Каменяр", "B");
        await db.Persons.AddRangeAsync(pA, pB);

        // plan with actions: A -> Dispatch (last is Dispatch), B -> Return
        var plan = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-101", State = PlanState.Open, RecordedUtc = DateTime.UtcNow };
        await db.Plans.AddAsync(plan);
        await db.SaveChangesAsync();

        var a1 = NewPlanAction(plan, pA, PlanActionType.Dispatch, DateTime.UtcNow.AddHours(-2), "Виїхав");
        var a2 = NewPlanAction(plan, pB, PlanActionType.Return, DateTime.UtcNow.AddHours(-1), "Повернувся");
        await db.PlanActions.AddRangeAsync(a1, a2);
        await db.SaveChangesAsync();

        var req = new CreatePublishDailyOrderViewModel
        {
            Name = "НК-ALL",
            EffectiveMomentUtc = DateTime.UtcNow,
            Author = "duty",
            PlanIds = [plan.Id],
            IncludePlanActionIds = null,                 // ← включити ВСЕ
            AutoReturnForOpenDispatch = true
        };

        // Act
        var executed = await svc.CreateAsync(req);

        // Assert
        // наказ створено
        Assert.NotEqual(Guid.Empty, executed.Order.Id);
        Assert.Equal("НК-ALL", executed.Order.Name);

        // план закрито і прив’язано
        var planReloaded = await db.Plans.FindAsync(plan.Id);
        Assert.Equal(PlanState.Close, planReloaded!.State);
        Assert.Equal(executed.Order.Id, planReloaded.OrderId);

        // у наказі має бути 3 дії: a1, a2, а також auto-Return для pA
        Assert.Equal(3, executed.ConfirmedActions.Count);

        // auto-Return: знайдемо запис Return для pA з EventAtUtc == EffectiveMomentUtc
        var order = await db.Orders.FindAsync(executed.Order.Id);
        var autoReturn = await db.OrderActions
            .Where(x => x.OrderId == order!.Id && x.PersonId == pA.Id && x.ActionType == PlanActionType.Return)
            .FirstOrDefaultAsync();
        Assert.NotNull(autoReturn);
        Assert.Equal(DateTimeKind.Utc, autoReturn!.EventAtUtc.Kind);
    }

    [Fact]
    public async Task CreateAsync_WithIncludeIds_ConfirmsOnlySpecified_AutoReturnNotIncluded()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;
        var svc = new OrderService(db);

        var pA = NewPerson("1111111111", "Каменяр", "А");
        await db.Persons.AddAsync(pA);

        var plan = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-201", State = PlanState.Open, RecordedUtc = DateTime.UtcNow };
        await db.Plans.AddAsync(plan);
        await db.SaveChangesAsync();

        // last is Dispatch -> autoReturn буде створено в плані, але в наказ не повинен піти
        var disp = NewPlanAction(plan, pA, PlanActionType.Dispatch, DateTime.UtcNow.AddMinutes(-30), "Виїхав");
        await db.PlanActions.AddAsync(disp);
        await db.SaveChangesAsync();

        var req = new CreatePublishDailyOrderViewModel
        {
            Name = "НК-INC",
            EffectiveMomentUtc = DateTime.UtcNow,
            Author = null,
            PlanIds = [plan.Id],
            IncludePlanActionIds = [disp.Id],   // підтверджуємо ЛИШЕ Dispatch
            AutoReturnForOpenDispatch = true
        };

        // Act
        var executed = await svc.CreateAsync(req);

        // Assert
        // наказ має містити 1 ConfirmedAction (Dispatch), autoReturn не включений
        Assert.Single(executed.ConfirmedActions);
        Assert.Equal(disp.Id, executed.ConfirmedActions[0].PlanActionId);

        // але в самому плані тепер ДВІ дії (Dispatch + авто Return)
        var planActions = await db.PlanActions.Where(x => x.PlanId == plan.Id).ToListAsync();
        Assert.Equal(2, planActions.Count);
        Assert.Contains(planActions, x => x.ActionType == PlanActionType.Return);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_NoOpenPlans()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;
        var svc = new OrderService(db);

        // закритий план (не має потрапити під вибірку)
        var closed = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-X", State = PlanState.Close, RecordedUtc = DateTime.UtcNow };
        await db.Plans.AddAsync(closed);
        await db.SaveChangesAsync();

        var req = new CreatePublishDailyOrderViewModel
        {
            Name = "НК-EMPTY",
            EffectiveMomentUtc = DateTime.UtcNow,
            Author = "duty",
            PlanIds = [closed.Id], // немає відкритих без наказу
            IncludePlanActionIds = null,
            AutoReturnForOpenDispatch = true
        };

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(req));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenOrder_HasPlans()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;

        var order = new Order { Id = Guid.NewGuid(), Name = "НК-DEL-P", EffectiveMomentUtc = DateTime.UtcNow };
        await db.Orders.AddAsync(order);

        var plan = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-DEL", State = PlanState.Close, Order = order, OrderId = order.Id };
        await db.Plans.AddAsync(plan);
        await db.SaveChangesAsync();

        var svc = new OrderService(db);

        // Act
        var ok = await svc.DeleteAsync(order.Id);

        // Assert
        Assert.False(ok);
        Assert.Equal(1, await db.Orders.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenOrder_HasActions()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;

        var order = new Order { Id = Guid.NewGuid(), Name = "НК-DEL-A", EffectiveMomentUtc = DateTime.UtcNow };
        await db.Orders.AddAsync(order);

        // потрібні valid FK: plan + person + planAction
        var person = NewPerson("3333333333", "Бондаренко", "Олег");
        await db.Persons.AddAsync(person);

        var plan = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-A", State = PlanState.Open, RecordedUtc = DateTime.UtcNow };
        await db.Plans.AddAsync(plan);
        await db.SaveChangesAsync();

        var pa = NewPlanAction(plan, person, PlanActionType.Dispatch, DateTime.UtcNow.AddMinutes(-10));
        await db.PlanActions.AddAsync(pa);
        await db.SaveChangesAsync();

        var oa = new OrderAction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            PlanId = plan.Id,
            PlanActionId = pa.Id,
            PersonId = person.Id,
            Person = person,
            ActionType = pa.ActionType,
            EventAtUtc = pa.EventAtUtc,
            Location = pa.Location,
            GroupName = pa.GroupName,
            CrewName = pa.CrewName,
            Rnokpp = pa.Rnokpp,
            FullName = pa.FullName,
            RankName = pa.RankName,
            PositionName = pa.PositionName,
            BZVP = pa.BZVP,
            Weapon = pa.Weapon,
            Callsign = pa.Callsign,
            StatusKindOnDate = pa.StatusKindOnDate
        };
        await db.OrderActions.AddAsync(oa);
        await db.SaveChangesAsync();

        var svc = new OrderService(db);

        // Act
        var ok = await svc.DeleteAsync(order.Id);

        // Assert
        Assert.False(ok);
        Assert.Equal(1, await db.Orders.CountAsync());
        Assert.Equal(1, await db.OrderActions.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_Deletes_WhenNoPlans_NoActions()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;

        var order = new Order { Id = Guid.NewGuid(), Name = "НК-DEL-OK", EffectiveMomentUtc = DateTime.UtcNow };
        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();

        var svc = new OrderService(db);

        // Act
        var ok = await svc.DeleteAsync(order.Id);

        // Assert
        Assert.True(ok);
        Assert.Equal(0, await db.Orders.CountAsync());
    }
}
