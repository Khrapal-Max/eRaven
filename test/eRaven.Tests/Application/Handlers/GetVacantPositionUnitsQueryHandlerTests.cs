//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetVacantPositionUnitsQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Handlers;
using eRaven.Application.Queries;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.PositionUnitRepository;
using eRaven.Tests.Extensions;

namespace eRaven.Tests.Application.Handlers;

public sealed class GetVacantPositionUnitsQueryHandlerTests : IAsyncLifetime
{
    private SqliteTestDb _db = default!;

    public Task InitializeAsync()
    {
        _db = new SqliteTestDb();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private async Task SeedAsync(params PositionUnit[] items)
    {
        await using var ctx = _db.Factory.CreateDbContext();
        ctx.PositionUnits.AddRange(items);
        await ctx.SaveChangesAsync();
    }

    private static PositionUnit PU(int number, string code, string shortName, string fullName, PositionUnitState state, bool isActived = true)
        => new()
        {
            Id = Guid.NewGuid(),
            Number = number,
            Code = code,
            ShortName = shortName,
            FullName = fullName,
            SpecialNumber = "SN",
            State = state,
            Rank = "R",
            Tarif = "T",
            IsActived = isActived
        };

    [Fact]
    public async Task HandleAsync_should_return_only_active_vacant_ordered_by_number_and_apply_take()
    {
        var v1 = PU(20, "A-20", "S20", "Full 20", PositionUnitState.Vacant, true);
        var v2 = PU(10, "A-10", "S10", "Full 10", PositionUnitState.Vacant, true);
        var v3 = PU(30, "A-30", "S30", "Full 30", PositionUnitState.Vacant, true);

        var notActive = PU(5, "X-05", "SX", "Full X", PositionUnitState.Vacant, false);
        var occupied = PU(15, "B-15", "SB", "Full B", PositionUnitState.Occupied, true);

        await SeedAsync(v1, v2, v3, notActive, occupied);

        var repo = new PositionUnitRepository(_db.Factory);
        var sut = new GetVacantPositionUnitsQueryHandler(repo);

        var result = await sut.HandleAsync(new GetVacantPositionUnitsQuery(Search: null, Take: 2));

        Assert.Equal(2, result.Count);
        Assert.Equal(v2.Id, result[0].Id);
        Assert.Equal(v1.Id, result[1].Id);
    }

    [Fact]
    public async Task HandleAsync_when_search_matches_code_should_filter()
    {
        var match = PU(1, "ABC-777", "Alpha", "Full Alpha", PositionUnitState.Vacant, true);
        var other = PU(2, "DEF-111", "Delta", "Full Delta", PositionUnitState.Vacant, true);
        await SeedAsync(match, other);

        var repo = new PositionUnitRepository(_db.Factory);
        var sut = new GetVacantPositionUnitsQueryHandler(repo);

        var result = await sut.HandleAsync(new GetVacantPositionUnitsQuery(Search: " 777 ", Take: 50));

        var one = Assert.Single(result);
        Assert.Equal(match.Id, one.Id);
        Assert.Equal("ABC-777", one.Code);
    }

    [Fact]
    public async Task HandleAsync_when_search_matches_shortname_or_fullname_should_filter()
    {
        var byShort = PU(1, "C-1", "Sniper", "Full 1", PositionUnitState.Vacant, true);
        var byFull = PU(2, "C-2", "Other", "Headquarters Driver", PositionUnitState.Vacant, true);
        var noMatch = PU(3, "C-3", "None", "Nothing", PositionUnitState.Vacant, true);

        await SeedAsync(byShort, byFull, noMatch);

        var repo = new PositionUnitRepository(_db.Factory);
        var sut = new GetVacantPositionUnitsQueryHandler(repo);

        var r1 = await sut.HandleAsync(new GetVacantPositionUnitsQuery(Search: "Snip", Take: 50));
        var r2 = await sut.HandleAsync(new GetVacantPositionUnitsQuery(Search: "Driver", Take: 50));

        Assert.Single(r1);
        Assert.Equal(byShort.Id, r1[0].Id);

        Assert.Single(r2);
        Assert.Equal(byFull.Id, r2[0].Id);
    }
}
