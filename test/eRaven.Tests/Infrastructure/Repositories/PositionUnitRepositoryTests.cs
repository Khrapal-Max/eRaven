//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitRepositoryTests -> PositionUnitRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.PositionUnitRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public class PositionUnitRepositoryTests : IAsyncLifetime
{
    private SqliteTestDb _db = default!;
    private PositionUnitRepository _repo = default!;

    public Task InitializeAsync()
    {
        _db = new SqliteTestDb();
        _repo = new PositionUnitRepository(_db.Factory);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    private static PositionUnit NewPosition(
        Guid? id = null,
        int number = 1,
        string code = "POS001",
        string shortName = "Short",
        string fullName = "Full name",
        string specialNumber = "1234567",
        PositionUnitState state = PositionUnitState.Vacant,
        string rank = "Rank",
        string tarif = "1",
        bool isActived = true)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Number = number,
            Code = code,
            ShortName = shortName,
            FullName = fullName,
            SpecialNumber = specialNumber,
            State = state,
            Rank = rank,
            Tarif = tarif,
            IsActived = isActived
        };

    [Fact]
    public async Task GetAllPositionUnits_returns_all_rows()
    {
        // Arrange
        var a = NewPosition(number: 1, code: "POS001", isActived: true);
        var b = NewPosition(number: 2, code: "POS002", isActived: true);

        await using (var ctx = await _db.Factory.CreateDbContextAsync())
        {
            ctx.PositionUnits.AddRange(a, b);
            await ctx.SaveChangesAsync();
        }

        // Act
        var result = (await _repo.GetAllPositionUnitsAsync(CancellationToken.None)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Id == a.Id && x.IsActived);
        Assert.Contains(result, x => x.Id == b.Id && x.IsActived);
    }

    [Fact]
    public async Task AddPositionUnit_adds_row_to_db()
    {
        // Arrange
        var item = NewPosition(number: 10, code: "POS010", isActived: true);

        // Act
        await _repo.AddPositionUnitAsync(item, CancellationToken.None);

        // Assert
        await using var ctx = await _db.Factory.CreateDbContextAsync();
        var fromDb = await ctx.PositionUnits.AsNoTracking().SingleOrDefaultAsync(x => x.Id == item.Id);

        Assert.NotNull(fromDb);
        Assert.Equal(item.Id, fromDb!.Id);
        Assert.Equal(item.Number, fromDb.Number);
        Assert.Equal(item.Code, fromDb.Code);
        Assert.Equal(item.ShortName, fromDb.ShortName);
        Assert.Equal(item.FullName, fromDb.FullName);
        Assert.Equal(item.SpecialNumber, fromDb.SpecialNumber);
        Assert.Equal(item.Rank, fromDb.Rank);
        Assert.Equal(item.Tarif, fromDb.Tarif);
        Assert.True(fromDb.IsActived);
    }

    [Fact]
    public async Task DeActivatedPositionUnit_sets_IsActived_false()
    {
        // Arrange
        var id = Guid.NewGuid();
        var item = NewPosition(id: id, number: 3, code: "POS003", isActived: true);

        await using (var ctx = await _db.Factory.CreateDbContextAsync())
        {
            ctx.PositionUnits.Add(item);
            await ctx.SaveChangesAsync();
        }

        // Act
        await _repo.DeActivatedPositionUnitAsync(id, CancellationToken.None);

        // Debug read from repo-context? read again in new ctx
        await using var verify = await _db.Factory.CreateDbContextAsync();
        var fromDb = await verify.PositionUnits.SingleAsync(x => x.Id == id);

        // Assert
        Assert.False(fromDb.IsActived);
    }

    [Fact]
    public async Task DeActivatedPositionUnit_when_not_found_throws()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act + Assert (position! -> NullReferenceException today)
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _repo.DeActivatedPositionUnitAsync(missingId, CancellationToken.None));
    }

    [Fact]
    public async Task DeActivatedPositionUnit_when_not_vacant_throws()
    {
        // Arrange
        var item = NewPosition(id: Guid.NewGuid(), number: 7, code: "POS012", state: PositionUnitState.Occupied, isActived: true);

        await using (var ctx = await _db.Factory.CreateDbContextAsync())
        {
            ctx.PositionUnits.Add(item);
            await ctx.SaveChangesAsync();
        }

        // Act + Assert (position! -> NullReferenceException today)
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _repo.DeActivatedPositionUnitAsync(item.Id, CancellationToken.None));
    }
}
