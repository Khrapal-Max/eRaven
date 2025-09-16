//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanServiceTests
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PlanService;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using eRaven.Tests.Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Tests.Services;

public class PlanServiceTests
{
    private static async Task SeedAsync(AppDbContext db, params Plan[] plans)
    {
        await db.Plans.AddRangeAsync(plans);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllPlanAsync_Returns_OrderedByRecordedUtc_Desc()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;

        var older = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-001", RecordedUtc = DateTime.UtcNow.AddHours(-2) };
        var middle = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-002", RecordedUtc = DateTime.UtcNow.AddHours(-1) };
        var newer = new Plan { Id = Guid.NewGuid(), PlanNumber = "PL-003", RecordedUtc = DateTime.UtcNow };
        await SeedAsync(db, older, middle, newer);

        var svc = new PlanService(db);

        // Act
        var result = await svc.GetAllPlanAsync();

        // Assert
        var list = result.ToList();
        Assert.Equal(3, list.Count);
        Assert.Equal("PL-003", list[0].PlanNumber);
        Assert.Equal("PL-002", list[1].PlanNumber);
        Assert.Equal("PL-001", list[2].PlanNumber);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_ViewModel_WhenExists()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;

        var id = Guid.NewGuid();
        var plan = new Plan
        {
            Id = id,
            PlanNumber = "PL-010",
            Author = "author",
            State = PlanState.Open,
            RecordedUtc = DateTime.UtcNow.AddMinutes(-5)
        };
        await SeedAsync(db, plan);

        var svc = new PlanService(db);

        // Act
        var vm = await svc.GetByIdAsync(id);

        // Assert
        Assert.NotNull(vm);
        Assert.Equal(id, vm!.Id);
        Assert.Equal("PL-010", vm.PlanNumber);
        Assert.Equal(PlanState.Open, vm.State);
        Assert.Equal("author", vm.Author);
        // ViewModel завжди з Kind=Utc (маппер це гарантує)
        Assert.Equal(DateTimeKind.Utc, vm.RecordedUtc.Kind);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;
        var svc = new PlanService(db);

        // Act
        var vm = await svc.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(vm);
    }

    [Fact]
    public async Task CreateAsync_CreatesPlan_OpenState_ReturnsViewModel()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;
        var svc = new PlanService(db);
        var before = DateTime.UtcNow;
        var req = new CreatePlanViewModel { PlanNumber = "PL-100", Author = "tester" };

        // Act
        var created = await svc.CreateAsync(req);

        // Assert
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("PL-100", created.PlanNumber);
        Assert.Equal(PlanState.Open, created.State);
        Assert.Equal("tester", created.Author);
        Assert.Equal(DateTimeKind.Utc, created.RecordedUtc.Kind);

        // verify persisted
        var entity = await db.Plans.FindAsync(created.Id);
        Assert.NotNull(entity);
        Assert.Equal("PL-100", entity!.PlanNumber);
        Assert.True(entity.RecordedUtc >= before);
    }

    [Fact]
    public async Task DeleteAsync_Removes_OpenPlan_WithoutOrder_And_ItsActions()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;

        var planId = Guid.NewGuid();
        var plan = new Plan
        {
            Id = planId,
            PlanNumber = "PL-200",
            State = PlanState.Open,
            RecordedUtc = DateTime.UtcNow
        };

        // потрібні Person з обов’язковими полями (Rank/First/Last/Rnokpp/BZVP)
        var pid1 = Guid.NewGuid();
        var pid2 = Guid.NewGuid();
        var person1 = new Person { Id = pid1, Rnokpp = "1111111111", Rank = "R", LastName = "A", FirstName = "A", BZVP = "Y" };
        var person2 = new Person { Id = pid2, Rnokpp = "2222222222", Rank = "R", LastName = "B", FirstName = "B", BZVP = "Y" };
        await db.Persons.AddRangeAsync(person1, person2);
        await db.Plans.AddAsync(plan);
        await db.SaveChangesAsync();

        var a1 = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            Plan = plan,
            PersonId = pid1,
            Person = person1,
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.UtcNow.AddMinutes(-10),
            Location = "L1",
            GroupName = "G1",
            CrewName = "C1",
            Rnokpp = "1111111111",
            FullName = "A A",
            RankName = "R",
            PositionName = "Командир відділення",
            BZVP = "Y",
            Weapon = "",
            Callsign = "",
            StatusKindOnDate = "S1"
        };
        var a2 = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            Plan = plan,
            PersonId = pid2,
            Person = person2,
            ActionType = PlanActionType.Return,
            EventAtUtc = DateTime.UtcNow.AddMinutes(-5),
            Location = "L2",
            GroupName = "G2",
            CrewName = "C2",
            Rnokpp = "2222222222",
            FullName = "B B",
            RankName = "R",
            PositionName = "Командир відділення",
            BZVP = "Y",
            Weapon = "",
            Callsign = "",
            StatusKindOnDate = "S2"
        };
        plan.PlanActions.Add(a1);
        plan.PlanActions.Add(a2);
        await db.PlanActions.AddRangeAsync(a1, a2);
        await db.SaveChangesAsync();

        var svc = new PlanService(db);

        // Act
        var ok = await svc.DeleteAsync(planId);

        // Assert
        Assert.True(ok);
        Assert.Equal(0, await db.Plans.CountAsync());
        Assert.Equal(0, await db.PlanActions.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenPlan_Closed()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanNumber = "PL-300",
            State = PlanState.Close,
            RecordedUtc = DateTime.UtcNow
        };
        await SeedAsync(db, plan);

        var svc = new PlanService(db);

        // Act
        var ok = await svc.DeleteAsync(plan.Id);

        // Assert
        Assert.False(ok);
        Assert.Equal(1, await db.Plans.CountAsync());
    }
    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenPlan_HasOrder()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;

        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            Name = "НК-2025/XYZ",
            EffectiveMomentUtc = DateTime.UtcNow,
            Author = "tester"
        };
        await db.Orders.AddAsync(order);

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanNumber = "PL-301",
            State = PlanState.Open,
            OrderId = orderId,
            Order = order,                  // (не обов’язково, але корисно для консистентності)
            RecordedUtc = DateTime.UtcNow
        };
        await db.Plans.AddAsync(plan);
        await db.SaveChangesAsync();

        var svc = new PlanService(db);

        // Act
        var ok = await svc.DeleteAsync(plan.Id);

        // Assert
        Assert.False(ok);
        Assert.Equal(1, await db.Plans.CountAsync());
        Assert.Equal(1, await db.Orders.CountAsync());  // переконуємось, що наказ існує
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenPlan_NotFound()
    {
        // Arrange
        using var dbh = new SqliteDbHelper();
        var db = dbh.Db;
        var svc = new PlanService(db);

        // Act
        var ok = await svc.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(ok);
    }
}
