//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitionServiceTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Tests.Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Tests.Services;

public class StatusTransitionServiceTests
{
    private static StatusKind K(string name, string code, int order = 0, bool isActive = true) =>
        new() { Name = name, Code = code, Order = order, IsActive = isActive, Author = "t", Modified = DateTime.UtcNow };

    private static StatusTransition T(int from, int to) =>
        new() { FromStatusKindId = from, ToStatusKindId = to };

    [Fact]
    public async Task GetAllMapAsync_Returns_Map_Grouped_By_FromId()
    {
        using var h = new SqliteDbHelper();
        await using var db = h.CreateContext();

        // Arrange kinds without explicit Ids
        var a = K("A", "A");
        var b = K("B", "B");
        var c = K("C", "C");
        db.StatusKinds.AddRange(a, b, c);
        await db.SaveChangesAsync();

        var aId = a.Id;
        var bId = b.Id;
        var cId = c.Id;

        // Add transitions just for our kinds
        db.StatusTransitions.AddRange(
            T(aId, bId), T(aId, cId),
            T(bId, cId)
        );
        await db.SaveChangesAsync();

        var sut = new StatusTransitionService(h.Factory);

        // Act
        var map = await sut.GetAllMapAsync();

        // Assert only our keys/relations (ігноруємо seed-дані з іншими ключами)
        Assert.True(map.ContainsKey(aId));
        Assert.True(map.ContainsKey(bId));

        // a -> {b, c}
        var aTos = map[aId];
        Assert.Contains(bId, aTos);
        Assert.Contains(cId, aTos);

        // b -> {c}
        var bTos = map[bId];
        Assert.Contains(cId, bTos);
    }

    [Fact]
    public async Task GetToIdsAsync_Returns_ToIds_For_SpecificFrom()
    {
        using var h = new SqliteDbHelper();
        await using var db = h.CreateContext();

        // Arrange: додаємо три статуси (Id згенеруються БД)
        var a = K("A", "A");
        var b = K("B", "B");
        var c = K("C", "C");
        db.StatusKinds.AddRange(a, b, c);
        await db.SaveChangesAsync();

        var aId = a.Id;
        var bId = b.Id;
        var cId = c.Id;

        // Лінки лише для a -> {b, c}
        db.StatusTransitions.AddRange(
            T(aId, bId),
            T(aId, cId)
        );
        await db.SaveChangesAsync();

        var sut = new StatusTransitionService(h.Factory);

        // Act
        var toIds = await sut.GetToIdsAsync(aId);

        // Assert: рівно два цілі (наші) і вони містять bId та cId
        Assert.Equal(2, toIds.Count);
        Assert.Contains(bId, toIds);
        Assert.Contains(cId, toIds);
    }

    [Fact]
    public async Task SaveAllowedAsync_Adds_New_And_Removes_Obsolete()
    {
        using var h = new SqliteDbHelper();
        await using var db = h.CreateContext();

        var a = K("A", "A");
        var b = K("B", "B");
        var c = K("C", "C");
        db.StatusKinds.AddRange(a, b, c);
        await db.SaveChangesAsync();

        var aId = a.Id;
        var bId = b.Id;
        var cId = c.Id;

        // Initially a->b
        db.StatusTransitions.Add(T(aId, bId));
        await db.SaveChangesAsync();

        var sut = new StatusTransitionService(h.Factory);

        // Save {c} only: remove a->b, add a->c
        await sut.SaveAllowedAsync(aId, [cId]);

        var rows = await db.StatusTransitions.AsNoTracking()
            .Where(t => t.FromStatusKindId == aId)
            .OrderBy(t => t.ToStatusKindId)
            .ToListAsync();

        Assert.Single(rows);
        Assert.Equal(cId, rows[0].ToStatusKindId);
    }

    [Fact]
    public async Task SaveAllowedAsync_Filters_SelfLoop()
    {
        using var h = new SqliteDbHelper();
        await using var db = h.CreateContext();

        var a = K("A", "A");
        var b = K("B", "B");
        db.StatusKinds.AddRange(a, b);
        await db.SaveChangesAsync();

        var aId = a.Id;
        var bId = b.Id;

        var sut = new StatusTransitionService(h.Factory);

        // Ask to allow {a,b}; self-loop a->a must be ignored; only a->b remains
        await sut.SaveAllowedAsync(aId, [aId, bId]);

        var rows = await db.StatusTransitions.AsNoTracking()
            .Where(t => t.FromStatusKindId == aId)
            .ToListAsync();

        Assert.Single(rows);
        Assert.Equal(bId, rows[0].ToStatusKindId);
    }

    [Fact]
    public async Task SaveAllowedAsync_Idempotent_When_No_Changes()
    {
        using var h = new SqliteDbHelper();
        await using var db = h.CreateContext();

        var a = K("A", "A");
        var b = K("B", "B");
        var c = K("C", "C");
        db.StatusKinds.AddRange(a, b, c);
        await db.SaveChangesAsync();

        var aId = a.Id;
        var bId = b.Id;
        var cId = c.Id;

        db.StatusTransitions.AddRange(T(aId, bId), T(aId, cId));
        await db.SaveChangesAsync();

        var sut = new StatusTransitionService(h.Factory);

        await sut.SaveAllowedAsync(aId, [bId, cId]);

        var rows = await db.StatusTransitions.AsNoTracking()
            .Where(t => t.FromStatusKindId == aId)
            .OrderBy(t => t.ToStatusKindId)
            .ToListAsync();

        Assert.Equal(2, rows.Count);
        Assert.Equal(bId, rows[0].ToStatusKindId);
        Assert.Equal(cId, rows[1].ToStatusKindId);
    }

    [Fact]
    public async Task SaveAllowedAsync_Removes_All_When_Empty_Set()
    {
        using var h = new SqliteDbHelper();
        await using var db = h.CreateContext();

        var a = K("A", "A");
        var b = K("B", "B");
        db.StatusKinds.AddRange(a, b);
        await db.SaveChangesAsync();

        var aId = a.Id;
        var bId = b.Id;

        db.StatusTransitions.Add(T(aId, bId));
        await db.SaveChangesAsync();

        var sut = new StatusTransitionService(h.Factory);

        // Save empty set => must remove all for 'a'
        await sut.SaveAllowedAsync(aId, []);

        var rows = await db.StatusTransitions.AsNoTracking()
            .Where(t => t.FromStatusKindId == aId)
            .ToListAsync();

        Assert.Empty(rows);
    }
}
