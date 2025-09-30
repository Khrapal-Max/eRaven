//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusKindServiceTests
//-----------------------------------------------------------------------------

using System.Threading;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.ViewModels.StatusKindViewModels;
using eRaven.Domain.Models;
using eRaven.Tests.Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Tests.Services;

public class StatusKindServiceTests
{
    private static StatusKind NewKind(
        string name,
        string code,
        int order = 0,
        bool isActive = true) => new()
        {
            Name = name,
            Code = code,
            Order = order,
            IsActive = isActive,
            Author = "test",
            Modified = DateTime.UtcNow
        };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_Default_Returns_All_Sorted_Order_Then_Name()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        // Додаємо ТІЛЬКИ наші 3 записи (сидингові ігноруємо в асертах)
        var k1 = NewKind("Бета", "B", order: 2, isActive: true);
        var k2 = NewKind("Альфа", "A", order: 1, isActive: false);
        var k3 = NewKind("Гамма", "G", order: 1, isActive: true);

        db.StatusKinds.AddRange(k1, k2, k3);
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new StatusKindService(h.Factory);

        // Беремо весь список (разом із сидингом), як робить сервіс
        var all = await sut.GetAllAsync(includeInactive: true);

        // Виділяємо тільки наші 3 додані елементи по їх Id
        var onlyOur = all.Where(x => x.Id == k1.Id || x.Id == k2.Id || x.Id == k3.Id).ToList();

        // Перевіряємо очікуваний порядок СЕРЕД наших елементів:
        // спочатку Order, потім Name => (1,"Альфа"), (1,"Гамма"), (2,"Бета")
        Assert.Equal(3, onlyOur.Count);
        Assert.Collection(onlyOur,
            x => { Assert.Equal(1, x.Order); Assert.Equal("Альфа", x.Name); },
            x => { Assert.Equal(1, x.Order); Assert.Equal("Гамма", x.Name); },
            x => { Assert.Equal(2, x.Order); Assert.Equal("Бета", x.Name); }
        );
    }

    [Fact]
    public async Task GetAllAsync_OnlyActive_Returns_Only_Active()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        var a = NewKind("Active1", "A1", isActive: true);
        var i = NewKind("Inactive1", "I1", isActive: false);

        db.StatusKinds.AddRange(a, i);
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new StatusKindService(h.Factory);

        var all = await sut.GetAllAsync(includeInactive: false);

        // беремо тільки наші два
        var ours = all.Where(x => x.Id == a.Id || x.Id == i.Id).ToList();

        // очікуємо, що серед НАШИХ повернувся лише активний
        Assert.Single(ours);
        Assert.Equal(a.Id, ours[0].Id);
        Assert.True(ours[0].IsActive);
        Assert.Equal("Active1", ours[0].Name);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_When_Exists_Returns_Entity()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        var k = NewKind("X", "X1");
        db.StatusKinds.Add(k);
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new StatusKindService(h.Factory);

        var got = await sut.GetByIdAsync(k.Id);

        Assert.NotNull(got);
        Assert.Equal(k.Id, got!.Id);
        Assert.Equal("X", got.Name);
    }

    [Fact]
    public async Task GetByIdAsync_When_NotExists_Returns_Null()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        var sut = new StatusKindService(h.Factory);

        var got = await sut.GetByIdAsync(123456);

        Assert.Null(got);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_Throws_When_Name_Missing()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        var sut = new StatusKindService(h.Factory);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.CreateAsync(new CreateKindViewModel
            {
                Name = "   ",
                Code = "C1"
            })
        );

        Assert.Equal("newKindViewModel", ex.ParamName);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_Code_Missing()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        var sut = new StatusKindService(h.Factory);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.CreateAsync(new CreateKindViewModel
            {
                Name = "Valid name",
                Code = " "
            })
        );

        Assert.Equal("newKindViewModel", ex.ParamName);
    }

    [Fact]
    public async Task CreateAsync_Persists_And_Trims()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        var sut = new StatusKindService(h.Factory);

        var created = await sut.CreateAsync(new CreateKindViewModel
        {
            Name = "  New Kind  ",
            Code = "  NK  ",
            Order = 7,
            IsActive = true
        });

        Assert.True(created.Id > 0);
        Assert.Equal("New Kind", created.Name);
        Assert.Equal("NK", created.Code);
        Assert.Equal(7, created.Order);
        Assert.True(created.IsActive);

        var fromDb = await db.StatusKinds.AsNoTracking().FirstAsync(x => x.Id == created.Id);

        Assert.Equal("New Kind", fromDb.Name);
        Assert.Equal("NK", fromDb.Code);
        Assert.Equal(7, fromDb.Order);
        Assert.True(fromDb.IsActive);
    }

    // ---------- SetActiveAsync ----------

    [Fact]
    public async Task SetActiveAsync_NoChange_Returns_True_And_PersistsNothing()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        var k = NewKind("Same", "S1", isActive: true);
        db.StatusKinds.Add(k);
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new StatusKindService(h.Factory);

        var ok = await sut.SetActiveAsync(k.Id, isActive: true);

        Assert.True(ok);
        var reloaded = await db.StatusKinds.AsNoTracking().FirstAsync(x => x.Id == k.Id);
        Assert.True(reloaded.IsActive);
    }

    [Fact]
    public async Task SetActiveAsync_Toggles_And_Saves()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        var k = NewKind("Toggle", "T1", isActive: false);
        db.StatusKinds.Add(k);
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new StatusKindService(h.Factory);

        var ok = await sut.SetActiveAsync(k.Id, isActive: true);

        Assert.True(ok);

        var reloaded = await db.StatusKinds.AsNoTracking().FirstAsync(x => x.Id == k.Id);
        Assert.True(reloaded.IsActive);
    }

    [Fact]
    public async Task SetActiveAsync_ReturnsFalse_When_NotFound()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        var sut = new StatusKindService(h.Factory);

        var ok = await sut.SetActiveAsync(id: 999999, isActive: false);

        Assert.False(ok);
    }

    // ---------- UpdateOrderAsync ----------

    [Fact]
    public async Task UpdateOrderAsync_Updates_Order_And_Saves()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        var k = NewKind("Ordered", "OR1", order: 1);
        db.StatusKinds.Add(k);
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new StatusKindService(h.Factory);

        var ok = await sut.UpdateOrderAsync(k.Id, newOrder: 42);

        Assert.True(ok);

        var reloaded = await db.StatusKinds.AsNoTracking().FirstAsync(x => x.Id == k.Id);
        Assert.Equal(42, reloaded.Order);
    }

    [Fact]
    public async Task UpdateOrderAsync_ReturnsFalse_When_NotFound()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        var sut = new StatusKindService(h.Factory);

        var ok = await sut.UpdateOrderAsync(id: 123456, newOrder: 5);

        Assert.False(ok);
    }

    // ---------- NameExistsAsync / CodeExistsAsync ----------

    [Fact]
    public async Task NameExistsAsync_True_When_ExactMatch_Exists()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        db.StatusKinds.Add(NewKind("Dup", "D1"));
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new StatusKindService(h.Factory);

        Assert.True(await sut.NameExistsAsync("Dup"));
        Assert.False(await sut.NameExistsAsync("dup")); // чутливість до регістру — як у сервісі
        Assert.False(await sut.NameExistsAsync("Other"));
    }

    [Fact]
    public async Task CodeExistsAsync_True_When_ExactMatch_Exists()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        db.StatusKinds.Add(NewKind("Kind", "KX"));
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new StatusKindService(h.Factory);

        Assert.True(await sut.CodeExistsAsync("KX"));
        Assert.False(await sut.CodeExistsAsync("kx")); // чутливість до регістру
        Assert.False(await sut.CodeExistsAsync("ZZZ"));
    }
}
