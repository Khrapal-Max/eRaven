//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitRepositoryTests -> PositionUnitRepository
//-----------------------------------------------------------------------------


using eRaven.Domain.Entities;
using eRaven.Infrastructure.Repositories.PositionUnitRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public class PositionUnitRepositoryTests : IAsyncLifetime
{
    private SqliteTestDb _db = default!;
    private PositionUnitRepository _repo = default!;

    public async Task InitializeAsync()
    {
        _db = new SqliteTestDb();
        _repo = new PositionUnitRepository(_db.Factory);
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task GetAllPositionUnits_Tests()
    {
        // Arrange
        await using (var ctx = await _db.Factory.CreateDbContextAsync())
        {
            ctx.PositionUnits.AddRange(new PositionUnit { Id = Guid.NewGuid(), IsActived = true });
            await ctx.SaveChangesAsync();
        }

        // Act
        var result = (await _repo.GetAllPositionUnits(CancellationToken.None)).ToList();

        // Assert
        Assert.Single(result);
        Assert.All(result, x => Assert.True(x.IsActived));
    }

    [Fact]
    public async Task AddPositionUnit_adds_row_to_db()
    {
        // Arrange
        var item = new PositionUnit { Id = Guid.NewGuid(), IsActived = true };

        // Act
        await _repo.AddPositionUnit(item, CancellationToken.None);

        // Assert
        await using var ctx = await _db.Factory.CreateDbContextAsync();
        var fromDb = await ctx.PositionUnits.FirstOrDefaultAsync(x => x.Id == item.Id);

        Assert.NotNull(fromDb);
        Assert.True(fromDb!.IsActived);
    }

    [Fact]
    public async Task DeActivatedPositionUnit_sets_IsActived_false()
    {
        // Arrange
        var id = Guid.NewGuid();

        await using (var ctx = await _db.Factory.CreateDbContextAsync())
        {
            ctx.PositionUnits.Add(new PositionUnit { Id = id, IsActived = true });
            await ctx.SaveChangesAsync();
        }

        // Act
        await _repo.DeActivatedPositionUnit(id, CancellationToken.None);

        // Assert
        await using var verify = await _db.Factory.CreateDbContextAsync();
        var fromDb = await verify.PositionUnits.SingleAsync(x => x.Id == id);

        Assert.False(fromDb.IsActived);
    }
}
