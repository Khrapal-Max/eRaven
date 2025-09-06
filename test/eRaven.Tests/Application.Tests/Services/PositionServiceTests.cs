//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionServiceTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Application.Services.PositionService;
using eRaven.Tests.Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Tests.Services;

public class PositionServiceTests
{
    private static PositionUnit NewPosition(bool isActive = true, string shortName = "Pos")
        => new()
        {
            Id = Guid.NewGuid(),
            ShortName = shortName,
            IsActived = isActive
        };

    // -------- GetPositionsAsync --------

    [Fact]
    public async Task GetPositionsAsync_Default_ReturnsOnlyActive()
    {
        using var h = new SqliteDbHelper();
        var db = h.Db;
        db.Positions.AddRange(
            NewPosition(true, "A"),
            NewPosition(true, "B"),
            NewPosition(false, "C")
        );
        await db.SaveChangesAsync();

        var sut = new PositionService(db);

        var items = await sut.GetPositionsAsync(); // onlyActive = true

        Assert.Equal(2, items.Count);
        Assert.All(items, x => Assert.True(x.IsActived));
        Assert.DoesNotContain(items, x => x.ShortName == "C");
    }

    [Fact]
    public async Task GetPositionsAsync_OnlyActiveFalse_ReturnsAll()
    {
        using var h = new SqliteDbHelper();
        var db = h.Db;
        db.Positions.AddRange(
            NewPosition(true, "A"),
            NewPosition(false, "B")
        );
        await db.SaveChangesAsync();

        var sut = new PositionService(db);

        var items = await sut.GetPositionsAsync(onlyActive: false);

        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.ShortName == "A" && x.IsActived);
        Assert.Contains(items, x => x.ShortName == "B" && !x.IsActived);
    }

    // -------- GetByIdAsync --------

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        using var h = new SqliteDbHelper();
        var db = h.Db;
        var p = NewPosition(true, "X");
        db.Positions.Add(p);
        await db.SaveChangesAsync();

        var sut = new PositionService(db);

        var got = await sut.GetByIdAsync(p.Id);

        Assert.NotNull(got);
        Assert.Equal(p.Id, got!.Id);
        Assert.Equal("X", got.ShortName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        using var h = new SqliteDbHelper();
        var db = h.Db;
        var sut = new PositionService(db);

        var got = await sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(got);
    }

    // -------- CreatePositionAsync --------

    [Fact]
    public async Task CreatePositionAsync_Throws_When_ShortName_Missing()
    {
        using var h = new SqliteDbHelper();
        var db = h.Db;
        var sut = new PositionService(db);

        var ex = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sut.CreatePositionAsync(new PositionUnit { ShortName = "   " })
        );

        Assert.Equal("positionUnit", ex.ParamName);
    }

    [Fact]
    public async Task CreatePositionAsync_Sets_IsActived_True_And_Persists()
    {
        using var h = new SqliteDbHelper();
        var db = h.Db;
        var sut = new PositionService(db);

        var created = await sut.CreatePositionAsync(new PositionUnit { ShortName = "New" });

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.True(created.IsActived);

        var inDb = await db.Positions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == created.Id);
        Assert.NotNull(inDb);
        Assert.True(inDb!.IsActived);
        Assert.Equal("New", inDb.ShortName);
    }

    // -------- SetActiveStateAsync --------

    [Fact]
    public async Task SetActiveStateAsync_ReturnsFalse_When_NotFound()
    {
        using var h = new SqliteDbHelper();
        var db = h.Db;
        var sut = new PositionService(db);

        var ok = await sut.SetActiveStateAsync(Guid.NewGuid(), isActive: false);

        Assert.False(ok);
    }

    [Fact]
    public async Task SetActiveStateAsync_NoChange_When_SameState()
    {
        using var h = new SqliteDbHelper();
        var db = h.Db;
        var pos = NewPosition(isActive: true, shortName: "Same");
        db.Positions.Add(pos);
        await db.SaveChangesAsync();

        var sut = new PositionService(db);

        var ok = await sut.SetActiveStateAsync(pos.Id, isActive: true);

        Assert.True(ok);
        var reloaded = await db.Positions.AsNoTracking().FirstAsync(x => x.Id == pos.Id);
        Assert.True(reloaded.IsActived); // стан не змінився
    }

    [Fact]
    public async Task SetActiveStateAsync_Deactivate_When_NotOccupied_Succeeds()
    {
        using var h = new SqliteDbHelper();
        var db = h.Db;
        var pos = NewPosition(isActive: true, shortName: "Free");
        db.Positions.Add(pos);
        await db.SaveChangesAsync();

        var sut = new PositionService(db);

        var ok = await sut.SetActiveStateAsync(pos.Id, isActive: false);

        Assert.True(ok);
        var reloaded = await db.Positions.AsNoTracking().FirstAsync(x => x.Id == pos.Id);
        Assert.False(reloaded.IsActived);
    }

    [Fact]
    public async Task SetActiveStateAsync_Deactivate_When_Occupied_Throws_And_PersistsNoChange()
    {
        using var h = new SqliteDbHelper();
        var db = h.Db;
        var pos = NewPosition(isActive: true, shortName: "Busy");
        db.Positions.Add(pos);

        var person = new Person
        {
            Id = Guid.NewGuid(),
            LastName = "Тест",
            FirstName = "Іван",
            StatusKindId = 1,
            PositionUnitId = pos.Id
        };
        db.Persons.Add(person);

        await db.SaveChangesAsync();

        var sut = new PositionService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.SetActiveStateAsync(pos.Id, isActive: false)
        );

        var reloaded = await db.Positions.AsNoTracking().FirstAsync(x => x.Id == pos.Id);
        Assert.True(reloaded.IsActived); // стан не змінився
    }

    [Fact]
    public async Task SetActiveStateAsync_Activate_Succeeds()
    {
        using var h = new SqliteDbHelper();
        var db = h.Db;
        var pos = NewPosition(isActive: false, shortName: "ReEnable");
        db.Positions.Add(pos);
        await db.SaveChangesAsync();

        var sut = new PositionService(db);

        var ok = await sut.SetActiveStateAsync(pos.Id, isActive: true);

        Assert.True(ok);
        var reloaded = await db.Positions.AsNoTracking().FirstAsync(x => x.Id == pos.Id);
        Assert.True(reloaded.IsActived);
    }
}
