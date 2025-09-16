//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActionServiceTests
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PlanActionService;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using eRaven.Tests.Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Tests.Services;

public sealed class PlanActionServiceTests
{
    // ===========================
    // Helpers
    // ===========================

    private static async Task SeedAsync(
        SqliteDbHelper dbh,
        Person? person = null,
        Plan? plan = null,
        IEnumerable<PlanAction>? actions = null,
        PositionUnit? unit = null)
    {
        var db = dbh.Db;

        if (unit is not null)
            db.PositionUnits.Add(unit);

        if (person is not null)
            db.Persons.Add(person);

        if (plan is not null)
            db.Plans.Add(plan);

        if (actions is not null)
            db.PlanActions.AddRange(actions);

        await db.SaveChangesAsync();
    }

    /// <summary>Взяти статус із сиду за Id або Name (щоб не створювати дублі).</summary>
    private static async Task<StatusKind> GetSeedStatusAsync(AppDbContext db, int? id = null, string? name = null)
    {
        StatusKind? s = null;
        if (id is not null)
            s = await db.StatusKinds.FirstOrDefaultAsync(x => x.Id == id.Value);
        if (s is null && !string.IsNullOrWhiteSpace(name))
            s = await db.StatusKinds.FirstOrDefaultAsync(x => x.Name == name);

        if (s is null)
            throw new InvalidOperationException("Seeded StatusKind not found. Check your seeding!");

        return s;
    }

    private static Person MakePerson(Guid? id = null, StatusKind? status = null, PositionUnit? unit = null)
    {
        return new Person
        {
            Id = id ?? Guid.NewGuid(),
            Rnokpp = "1234567890",
            Rank = "Сержант",
            LastName = "Іваненко",
            FirstName = "Іван",
            MiddleName = "Іванович",
            BZVP = "так",
            Weapon = "АК-74",
            Callsign = "Камінь",
            CreatedUtc = DateTime.UtcNow.AddDays(-3),
            ModifiedUtc = DateTime.UtcNow.AddDays(-1),

            StatusKindId = status?.Id,
            StatusKind = status,

            PositionUnitId = unit?.Id,
            PositionUnit = unit
        };
    }

    private static Plan MakePlan(
        Guid? id = null,
        PlanState state = PlanState.Open,
        Guid? orderId = null,
        string? planNumber = null)
    {
        return new Plan
        {
            Id = id ?? Guid.NewGuid(),
            PlanNumber = planNumber ?? $"PL-{Guid.NewGuid():N}"[..8], // унікально для тестів
            State = state,
            Author = "tester",
            RecordedUtc = DateTime.UtcNow.AddHours(-12),
            OrderId = orderId
        };
    }

    private static PositionUnit MakeUnit() => new()
    {
        Id = Guid.NewGuid(),
        Code = $"U-{Guid.NewGuid():N}"[..6],
        ShortName = "Взвод",
        OrgPath = "1-й взвод"
    };

    private static PlanAction CloneForPlan(PlanAction src, Guid planId) => new()
    {
        Id = Guid.NewGuid(),
        PlanId = planId,
        PersonId = src.PersonId,
        ActionType = src.ActionType,
        EventAtUtc = src.EventAtUtc,
        Location = src.Location,
        GroupName = src.GroupName,
        CrewName = src.CrewName,
        Rnokpp = src.Rnokpp,
        FullName = src.FullName,
        RankName = src.RankName,
        PositionName = src.PositionName,
        BZVP = src.BZVP,
        Weapon = src.Weapon,
        Callsign = src.Callsign,
        StatusKindOnDate = src.StatusKindOnDate
    };

    // ===========================
    // GetByPlanAsync
    // ===========================

    [Fact]
    public async Task GetByPlanAsync_ReturnsActions_OrderedByEventAt()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;

        var plan = MakePlan();
        var status = await GetSeedStatusAsync(db, id: 1); // "В районі" зі сиду
        var person = MakePerson(status: status);

        var a1 = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            PersonId = person.Id,
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            Location = "Локація A",
            GroupName = "Група 1",
            CrewName = "Зміна 1",
            Rnokpp = person.Rnokpp,
            FullName = person.FullName,
            RankName = person.Rank,
            PositionName = "Посада X",
            BZVP = person.BZVP,
            Weapon = person.Weapon ?? "",
            Callsign = person.Callsign ?? "",
            StatusKindOnDate = status.Name
        };
        var a2 = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            PersonId = person.Id,
            ActionType = PlanActionType.Return,
            EventAtUtc = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Location = "Локація A",
            GroupName = "Група 1",
            CrewName = "Зміна 1",
            Rnokpp = person.Rnokpp,
            FullName = person.FullName,
            RankName = person.Rank,
            PositionName = "Посада X",
            BZVP = person.BZVP,
            Weapon = person.Weapon ?? "",
            Callsign = person.Callsign ?? "",
            StatusKindOnDate = status.Name
        };

        await SeedAsync(dbh, plan: plan, person: person, actions: [a2, a1]);

        var svc = new PlanActionService(db);

        // Act
        var list = await svc.GetByPlanAsync(plan.Id);

        // Assert
        Assert.Equal(2, list.Count);
        Assert.Equal(a1.Id, list[0].Id); // ordered ascending by EventAtUtc
        Assert.Equal(a2.Id, list[1].Id);
    }

    // ===========================
    // CreateAsync
    // ===========================

    [Fact]
    public async Task CreateAsync_CreatesAction_WithSnapshotFromPerson_AndUtcTime()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;

        var status = await GetSeedStatusAsync(db, name: "В районі");
        var unit = MakeUnit();
        var person = MakePerson(status: status, unit: unit);
        var plan = MakePlan();

        await SeedAsync(dbh, unit: unit, person: person, plan: plan);

        var svc = new PlanActionService(db);
        var vm = new CreatePlanActionViewModel
        {
            PlanId = plan.Id,
            PersonId = person.Id,
            ActionType = PlanActionType.Dispatch,
            // Kind може бути Unspecified — сервіс має нормалізувати до Utc (за значенням)
            EventAtUtc = new DateTime(2025, 1, 2, 10, 15, 0, DateTimeKind.Unspecified),
            Location = "Локація B",
            GroupName = "Група 2",
            CrewName = "Зміна 2"
        };

        // Act
        var created = await svc.CreateAsync(vm);

        // Assert
        var action = await db.PlanActions.AsNoTracking().SingleAsync(a => a.Id == created.Id);

        Assert.Equal(plan.Id, action.PlanId);
        Assert.Equal(person.Id, action.PersonId);
        Assert.Equal(vm.ActionType, action.ActionType);

        // SQLite повертає Kind=Unspecified — порівнюємо значення з примусовим Kind=Utc
        var expectedUtc = DateTime.SpecifyKind(vm.EventAtUtc, DateTimeKind.Utc);
        var storedUtc = DateTime.SpecifyKind(action.EventAtUtc, DateTimeKind.Utc);
        Assert.Equal(expectedUtc, storedUtc);

        // snapshot з Person
        Assert.Equal(person.Rnokpp, action.Rnokpp);
        Assert.Equal(person.FullName, action.FullName);
        Assert.Equal(person.Rank, action.RankName);
        Assert.Equal(unit.FullName, action.PositionName);
        Assert.Equal(person.BZVP, action.BZVP);
        Assert.Equal(person.Weapon, action.Weapon);
        Assert.Equal(person.Callsign, action.Callsign);
        Assert.Equal(status.Name, action.StatusKindOnDate);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenPlanNotFound()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var svc = new PlanActionService(dbh.Db);

        var vm = new CreatePlanActionViewModel
        {
            PlanId = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.UtcNow,
            Location = "L",
            GroupName = "G",
            CrewName = "C"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(vm));
        Assert.Contains("План не знайдено", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenPlanClosedOrHasOrder()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;

        var planClosed = MakePlan(state: PlanState.Close, planNumber: "PL-CLOSED-1");

        // реальний наказ для FK
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Name = "DO-TEST-1",
            EffectiveMomentUtc = DateTime.UtcNow,
            Author = "tester",
            RecordedUtc = DateTime.UtcNow
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var planWithOrder = MakePlan(orderId: order.Id, planNumber: "PL-ORDER-2");

        var status = await GetSeedStatusAsync(db, id: 1);
        var person = MakePerson(status: status);

        await SeedAsync(dbh, plan: planClosed, person: person);
        await SeedAsync(dbh, plan: planWithOrder);

        var svc = new PlanActionService(db);

        // Act & Assert (closed)
        var vmClosed = new CreatePlanActionViewModel
        {
            PlanId = planClosed.Id,
            PersonId = person.Id,
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.UtcNow,
            Location = "L",
            GroupName = "G",
            CrewName = "C"
        };
        var ex1 = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(vmClosed));
        Assert.Contains("лише до відкритого плану", ex1.Message, StringComparison.OrdinalIgnoreCase);

        // Act & Assert (has order)
        var vmWithOrder = vmClosed with { PlanId = planWithOrder.Id };
        var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(vmWithOrder));
        Assert.Contains("лише до відкритого плану", ex2.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenPersonNotFound()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var plan = MakePlan();
        await SeedAsync(dbh, plan: plan);

        var svc = new PlanActionService(dbh.Db);

        var vm = new CreatePlanActionViewModel
        {
            PlanId = plan.Id,
            PersonId = Guid.NewGuid(),
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.UtcNow,
            Location = "L",
            GroupName = "G",
            CrewName = "C"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(vm));
        Assert.Contains("Особа не знайдена", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ===========================
    // DeleteAsync
    // ===========================

    [Fact]
    public async Task DeleteAsync_RemovesAction_WhenPlanOpen()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var plan = MakePlan(state: PlanState.Open);
        var status = await GetSeedStatusAsync(dbh.Db, id: 1);
        var person = MakePerson(status: status);

        var act = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            PersonId = person.Id,
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.UtcNow,
            Location = "L",
            GroupName = "G",
            CrewName = "C",
            Rnokpp = person.Rnokpp,
            FullName = person.FullName,
            RankName = person.Rank,
            PositionName = "Посада",
            BZVP = "так",
            Weapon = "АК-74",
            Callsign = "Камінь",
            StatusKindOnDate = status.Name
        };

        await SeedAsync(dbh, plan: plan, person: person, actions: [act]);

        var svc = new PlanActionService(dbh.Db);

        // Act
        var ok = await svc.DeleteAsync(act.Id);

        // Assert
        Assert.True(ok);
        Assert.Equal(0, await dbh.Db.PlanActions.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenActionNotFound()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var svc = new PlanActionService(dbh.Db);

        // Act
        var ok = await svc.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(ok);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenPlanClosedOrHasOrder()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;

        // closed plan
        var planClosed = MakePlan(state: PlanState.Close, planNumber: "PL-CLOSED-DEL");
        var status = await GetSeedStatusAsync(db, id: 1);
        var person = MakePerson(status: status);

        var a1 = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = planClosed.Id,
            PersonId = person.Id,
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.UtcNow,
            Location = "L",
            GroupName = "G",
            CrewName = "C",
            Rnokpp = person.Rnokpp,
            FullName = person.FullName,
            RankName = person.Rank,
            PositionName = "Посада",
            BZVP = "так",
            Weapon = "АК-74",
            Callsign = "Камінь",
            StatusKindOnDate = status.Name
        };

        // наказ для FK
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Name = "DO-DEL-1",
            EffectiveMomentUtc = DateTime.UtcNow,
            Author = "tester",
            RecordedUtc = DateTime.UtcNow
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // plan with order
        var planWithOrder = MakePlan(orderId: order.Id, planNumber: "PL-ORDER-DEL");
        var a2 = CloneForPlan(a1, planWithOrder.Id);

        await SeedAsync(dbh, plan: planClosed, person: person, actions: [a1]);
        await SeedAsync(dbh, plan: planWithOrder, actions: [a2]);

        var svc = new PlanActionService(db);

        // Act
        var res1 = await svc.DeleteAsync(a1.Id); // план закритий
        var res2 = await svc.DeleteAsync(a2.Id); // план має наказ

        // Assert
        Assert.False(res1);
        Assert.False(res2);
        Assert.Equal(2, await db.PlanActions.CountAsync());
    }
}