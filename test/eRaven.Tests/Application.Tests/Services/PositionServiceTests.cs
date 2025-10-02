//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PositionServiceTests (updated for required Code/OrgPath/SpecialNumber)
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Tests.Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Tests.Services;

public class PositionServiceTests
{
    // Базові значення для обов’язкових колонок
    private const string DEF_CODE = "CODE-1";
    private const string DEF_ORG = "Unit / Dept";
    private const string DEF_SN = "11-111";

    private static PositionUnit NewPosition(
        bool isActive = true,
        string shortName = "Pos",
        string code = DEF_CODE,
        string orgPath = DEF_ORG,
        string special = DEF_SN)
        => new()
        {
            Id = Guid.NewGuid(),
            ShortName = shortName,
            IsActived = isActive,
            Code = code,
            OrgPath = orgPath,
            SpecialNumber = special
        };

    private static PositionUnit NewWithCode(string code, bool isActive = true, string shortName = "Pos")
        => NewPosition(isActive, shortName, code, DEF_ORG, DEF_SN);

    // -------- GetPositionsAsync --------

    [Fact]
    public async Task GetPositionsAsync_Default_ReturnsOnlyActive()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        db.PositionUnits.AddRange(
            NewWithCode("A", true, "A"),
            NewWithCode("B", true, "B"),
            NewWithCode("C", false, "C")
        );
        await db.SaveChangesAsync();

        var sut = new PositionService(h.Factory);

        var items = await sut.GetPositionsAsync(); // onlyActive = true

        Assert.Equal(2, items.Count);
        Assert.All(items, x => Assert.True(x.IsActived));
        Assert.DoesNotContain(items, x => x.ShortName == "C");
    }

    [Fact]
    public async Task GetPositionsAsync_OnlyActiveFalse_ReturnsAll()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        db.PositionUnits.AddRange(
            NewWithCode("A", true, "A"),
            NewWithCode("B", false, "B")
        );
        await db.SaveChangesAsync();

        var sut = new PositionService(h.Factory);

        var items = await sut.GetPositionsAsync(onlyActive: false);

        Assert.Single(items);
        Assert.Contains(items, x => x.ShortName == "A" && x.IsActived);
        Assert.DoesNotContain(items, x => x.ShortName == "B" && !x.IsActived);
    }

    // -------- GetByIdAsync --------

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        var p = NewWithCode("X", true, "X");
        db.PositionUnits.Add(p);
        await db.SaveChangesAsync();

        var sut = new PositionService(h.Factory);

        var got = await sut.GetByIdAsync(p.Id);

        Assert.NotNull(got);
        Assert.Equal(p.Id, got!.Id);
        Assert.Equal("X", got.ShortName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        var sut = new PositionService(h.Factory);

        var got = await sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(got);
    }

    // -------- CreatePositionAsync --------

    [Fact]
    public async Task CreatePositionAsync_Throws_When_ShortName_Missing()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        var sut = new PositionService(h.Factory);

        var ex = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sut.CreatePositionAsync(new PositionUnit
            {
                ShortName = "   ",
                Code = "X1",
                OrgPath = DEF_ORG,
                SpecialNumber = DEF_SN,
                IsActived = true
            })
        );

        Assert.Equal("positionUnit", ex.ParamName);
    }

    [Fact]
    public async Task CreatePositionAsync_Persists_WithoutForcingActive()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        var sut = new PositionService(h.Factory);

        // Створення НЕ повинно примусово активувати
        var created = await sut.CreatePositionAsync(new PositionUnit
        {
            ShortName = "New",
            Code = "A1",
            OrgPath = DEF_ORG,
            SpecialNumber = DEF_SN,
            IsActived = false
        });

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.False(created.IsActived);

        var inDb = await db.PositionUnits.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == created.Id);

        Assert.NotNull(inDb);
        Assert.False(inDb!.IsActived);
        Assert.Equal("New", inDb.ShortName);
        Assert.Equal("A1", inDb.Code);
        Assert.Equal(DEF_ORG, inDb.OrgPath);
        Assert.Equal(DEF_SN, inDb.SpecialNumber);
    }

    [Fact]
    public async Task CreatePositionAsync_Throws_When_DuplicateActiveCode()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        var sut = new PositionService(h.Factory);

        // Перша активна з кодом A1
        await sut.CreatePositionAsync(new PositionUnit
        {
            ShortName = "First",
            Code = "A1",
            OrgPath = DEF_ORG,
            SpecialNumber = DEF_SN,
            IsActived = true
        });

        // Друга активна з тим самим кодом -> має впасти
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreatePositionAsync(new PositionUnit
            {
                ShortName = "Second",
                Code = "A1",
                OrgPath = DEF_ORG,
                SpecialNumber = DEF_SN,
                IsActived = true
            }));
    }

    [Fact]
    public async Task CreatePositionAsync_Allows_DuplicateCode_IfFirstIsInactive()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        var sut = new PositionService(h.Factory);

        // Перша з кодом B1, але неактивна
        await sut.CreatePositionAsync(new PositionUnit
        {
            ShortName = "First",
            Code = "B1",
            OrgPath = DEF_ORG,
            SpecialNumber = DEF_SN,
            IsActived = false
        });

        // Друга активна з тим самим кодом — дозволено (бо активного дубля немає)
        var second = await sut.CreatePositionAsync(new PositionUnit
        {
            ShortName = "Second",
            Code = "B1",
            OrgPath = DEF_ORG,
            SpecialNumber = DEF_SN,
            IsActived = true
        });

        Assert.True(second.IsActived);
        Assert.Equal("B1", second.Code);
    }

    // -------- SetActiveStateAsync --------

    [Fact]
    public async Task SetActiveStateAsync_ReturnsFalse_When_NotFound()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        var sut = new PositionService(h.Factory);

        var ok = await sut.SetActiveStateAsync(Guid.NewGuid(), isActive: false);

        Assert.False(ok);
    }

    [Fact]
    public async Task SetActiveStateAsync_NoChange_When_SameState()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        var pos = NewWithCode("SAME", true, "Same");
        db.PositionUnits.Add(pos);
        await db.SaveChangesAsync();

        var sut = new PositionService(h.Factory);

        var ok = await sut.SetActiveStateAsync(pos.Id, isActive: true);

        Assert.True(ok);
        var reloaded = await db.PositionUnits.AsNoTracking().FirstAsync(x => x.Id == pos.Id);
        Assert.True(reloaded.IsActived); // стан не змінився
    }

    [Fact]
    public async Task SetActiveStateAsync_Deactivate_When_NotOccupied_Succeeds()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        var pos = NewWithCode("FREE1", true, "Free");
        db.PositionUnits.Add(pos);
        await db.SaveChangesAsync();

        var sut = new PositionService(h.Factory);

        var ok = await sut.SetActiveStateAsync(pos.Id, isActive: false);

        Assert.True(ok);
        var reloaded = await db.PositionUnits.AsNoTracking().FirstAsync(x => x.Id == pos.Id);
        Assert.False(reloaded.IsActived);
    }

    [Fact]
    public async Task SetActiveStateAsync_Deactivate_When_Occupied_Throws_And_PersistsNoChange()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        var pos = NewWithCode("BUSY1", true, "Busy");
        db.PositionUnits.Add(pos);

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

        var sut = new PositionService(h.Factory);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.SetActiveStateAsync(pos.Id, isActive: false)
        );

        var reloaded = await db.PositionUnits.AsNoTracking().FirstAsync(x => x.Id == pos.Id);
        Assert.True(reloaded.IsActived); // стан не змінився
    }

    [Fact]
    public async Task SetActiveStateAsync_Activate_Succeeds()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        var pos = NewWithCode("REEN1", false, "ReEnable");
        db.PositionUnits.Add(pos);
        await db.SaveChangesAsync();

        var sut = new PositionService(h.Factory);

        var ok = await sut.SetActiveStateAsync(pos.Id, isActive: true);

        Assert.True(ok);
        var reloaded = await db.PositionUnits.AsNoTracking().FirstAsync(x => x.Id == pos.Id);
        Assert.True(reloaded.IsActived);
    }

    [Fact]
    public async Task SetActiveStateAsync_Activate_Throws_When_AnotherActiveWithSameCodeExists()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        // Активна з кодом A1 вже існує
        var existingActive = NewWithCode("A1", true, "Active1");
        db.PositionUnits.Add(existingActive);

        // Друга з тим же кодом, але неактивна
        var toActivate = NewWithCode("A1", false, "InactiveSameCode");
        db.PositionUnits.Add(toActivate);

        await db.SaveChangesAsync();

        var sut = new PositionService(h.Factory);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.SetActiveStateAsync(toActivate.Id, isActive: true));
    }

    [Fact]
    public async Task SetActiveStateAsync_Activate_Succeeds_When_CodeIsUnique()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();

        // Активна з кодом A1
        db.PositionUnits.Add(NewWithCode("A1", true, "Active1"));

        // Інша з унікальним кодом
        var toActivateUnique = NewWithCode("U1", false, "Unique");
        db.PositionUnits.Add(toActivateUnique);

        await db.SaveChangesAsync();

        var sut = new PositionService(h.Factory);

        var ok = await sut.SetActiveStateAsync(toActivateUnique.Id, isActive: true);

        Assert.True(ok);

        var re2 = await db.PositionUnits.AsNoTracking().FirstAsync(x => x.Id == toActivateUnique.Id);
        Assert.True(re2.IsActived);
    }

    // -------- CodeExistsActiveAsync --------

    [Fact]
    public async Task CodeExistsActiveAsync_ReturnsTrue_WhenActivePositionWithCodeExists()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        db.PositionUnits.Add(NewWithCode("A1", true, "AAA"));
        await db.SaveChangesAsync();

        var sut = new PositionService(h.Factory);

        var result = await sut.CodeExistsActiveAsync("A1");

        Assert.True(result);
    }

    [Fact]
    public async Task CodeExistsActiveAsync_ReturnsFalse_WhenOnlyInactivePositionWithCodeExists()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        db.PositionUnits.Add(NewWithCode("B2", false, "BBB"));
        await db.SaveChangesAsync();

        var sut = new PositionService(h.Factory);

        var result = await sut.CodeExistsActiveAsync("B2");

        Assert.False(result);
    }

    [Fact]
    public async Task CodeExistsActiveAsync_ReturnsFalse_WhenNoPositionWithCode()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        var sut = new PositionService(h.Factory);

        var result = await sut.CodeExistsActiveAsync("ZZZ");

        Assert.False(result);
    }

    [Fact]
    public async Task CodeExistsActiveAsync_IgnoresWhitespace_And_IsCaseSensitive()
    {
        using var h = new SqliteDbHelper();
        var db = h.CreateContext();
        db.PositionUnits.Add(NewWithCode("C3", true, "CCC"));
        await db.SaveChangesAsync();

        var sut = new PositionService(h.Factory);

        var resultTrimmed = await sut.CodeExistsActiveAsync(" C3 ");
        var resultDifferentCase = await sut.CodeExistsActiveAsync("c3"); // якщо треба case-insensitive — міняємо сервіс

        Assert.True(resultTrimmed);
        Assert.False(resultDifferentCase);
    }
}
