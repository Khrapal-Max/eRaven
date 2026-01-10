//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitRepositoryTests -> PositionUnitRepository (synced with SqliteTestDb)
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.PositionUnitRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class PositionUnitRepositoryTests : IAsyncLifetime
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
        => await _db.DisposeAsync();

    // -------------------------
    // Helpers
    // -------------------------

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

    private async Task SeedAsync(params PositionUnit[] items)
    {
        await using var ctx = await _db.Factory.CreateDbContextAsync();
        ctx.PositionUnits.AddRange(items);
        await ctx.SaveChangesAsync();
    }

    private async Task<PositionUnit?> FindAsync(Guid id)
    {
        await using var ctx = await _db.Factory.CreateDbContextAsync();
        return await ctx.PositionUnits.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
    }

    // -------------------------
    // Tests: existing repo methods
    // -------------------------

    [Fact]
    public async Task GetAllPositionUnitsAsync_returns_all_rows()
    {
        // Arrange (unique codes!)
        var a = NewPosition(number: 1, code: "POS001");
        var b = NewPosition(number: 2, code: "POS002");

        await SeedAsync(a, b);

        // Act
        var result = (await _repo.GetAllPositionUnitsAsync(CancellationToken.None)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Id == a.Id);
        Assert.Contains(result, x => x.Id == b.Id);
    }

    [Fact]
    public async Task AddPositionUnitAsync_adds_row_to_db()
    {
        // Arrange
        var item = NewPosition(number: 10, code: "POS010", isActived: true);

        // Act
        await _repo.AddPositionUnitAsync(item, CancellationToken.None);

        // Assert
        var fromDb = await FindAsync(item.Id);

        Assert.NotNull(fromDb);
        Assert.Equal(item.Id, fromDb!.Id);
        Assert.Equal(item.Number, fromDb.Number);
        Assert.Equal(item.Code, fromDb.Code);
        Assert.Equal(item.ShortName, fromDb.ShortName);
        Assert.Equal(item.FullName, fromDb.FullName);
        Assert.Equal(item.SpecialNumber, fromDb.SpecialNumber);
        Assert.Equal(item.State, fromDb.State);
        Assert.Equal(item.Rank, fromDb.Rank);
        Assert.Equal(item.Tarif, fromDb.Tarif);
        Assert.True(fromDb.IsActived);
    }

    [Fact]
    public async Task DeActivatedPositionUnitAsync_sets_IsActived_false_when_vacant()
    {
        // Arrange
        var id = Guid.NewGuid();
        var item = NewPosition(id: id, number: 3, code: "POS003", state: PositionUnitState.Vacant, isActived: true);

        await SeedAsync(item);

        // Act
        await _repo.DeActivatedPositionUnitAsync(id, CancellationToken.None);

        // Assert
        await using var verify = await _db.Factory.CreateDbContextAsync();
        var fromDb = await verify.PositionUnits.SingleAsync(x => x.Id == id);
        Assert.False(fromDb.IsActived);
    }

    [Fact]
    public async Task DeActivatedPositionUnitAsync_when_not_found_throws_KeyNotFoundException()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _repo.DeActivatedPositionUnitAsync(missingId, CancellationToken.None));
    }

    [Fact]
    public async Task DeActivatedPositionUnitAsync_when_not_vacant_throws_InvalidOperationException()
    {
        // Arrange
        var item = NewPosition(
            id: Guid.NewGuid(),
            number: 7,
            code: "POS012",
            state: PositionUnitState.Occupied,
            isActived: true);

        await SeedAsync(item);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repo.DeActivatedPositionUnitAsync(item.Id, CancellationToken.None));
    }

    // -------------------------
    // Tests: new repo method GetVacantOptionsAsync
    // -------------------------

    [Fact]
    public async Task GetVacantOptionsAsync_returns_only_active_vacant_ordered_and_take()
    {
        // Arrange
        var v1 = NewPosition(number: 20, code: "V-20", state: PositionUnitState.Vacant, isActived: true, rank: "R1", tarif: "T1");
        var v2 = NewPosition(number: 10, code: "V-10", state: PositionUnitState.Vacant, isActived: true, rank: "R2", tarif: "T2");
        var v3 = NewPosition(number: 30, code: "V-30", state: PositionUnitState.Vacant, isActived: true);

        var inactiveVacant = NewPosition(number: 5, code: "IV-05", state: PositionUnitState.Vacant, isActived: false);
        var occupied = NewPosition(number: 15, code: "O-15", state: PositionUnitState.Occupied, isActived: true);
        var tempOcc = NewPosition(number: 16, code: "TO-16", state: PositionUnitState.TemporarilyOccupied, isActived: true);
        var tempCand = NewPosition(number: 17, code: "TC-17", state: PositionUnitState.TemporarilyCandidate, isActived: true);

        await SeedAsync(v1, v2, v3, inactiveVacant, occupied, tempOcc, tempCand);

        // Act
        var result = await _repo.GetVacantOptionsAsync(search: null, take: 2, ct: CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(v2.Id, result[0].Id); // Number=10
        Assert.Equal(v1.Id, result[1].Id); // Number=20

        Assert.Equal("V-10", result[0].Code);
        Assert.Equal("R2", result[0].Rank);
        Assert.Equal("T2", result[0].Tarif);
    }

    [Fact]
    public async Task GetVacantOptionsAsync_trims_search_and_filters_by_code_shortname_fullname()
    {
        // Arrange
        var byCode = NewPosition(number: 1, code: "ABC-777", shortName: "S1", fullName: "F1", state: PositionUnitState.Vacant, isActived: true);
        var byShort = NewPosition(number: 2, code: "DEF-111", shortName: "Sniper", fullName: "F2", state: PositionUnitState.Vacant, isActived: true);
        var byFull = NewPosition(number: 3, code: "GHI-222", shortName: "S3", fullName: "Headquarters Driver", state: PositionUnitState.Vacant, isActived: true);
        var noMatch = NewPosition(number: 4, code: "JKL-333", shortName: "X", fullName: "Y", state: PositionUnitState.Vacant, isActived: true);

        await SeedAsync(byCode, byShort, byFull, noMatch);

        // Act
        var r1 = await _repo.GetVacantOptionsAsync(search: " 777 ", take: 50, ct: CancellationToken.None);
        var r2 = await _repo.GetVacantOptionsAsync(search: "Snip", take: 50, ct: CancellationToken.None);
        var r3 = await _repo.GetVacantOptionsAsync(search: "Driver", take: 50, ct: CancellationToken.None);

        // Assert
        Assert.Single(r1);
        Assert.Equal(byCode.Id, r1[0].Id);

        Assert.Single(r2);
        Assert.Equal(byShort.Id, r2[0].Id);

        Assert.Single(r3);
        Assert.Equal(byFull.Id, r3[0].Id);
    }

    [Fact]
    public async Task GetVacantOptionsAsync_ignores_inactive_and_non_vacant_even_if_matches_search()
    {
        // Arrange
        var inactiveMatch = NewPosition(number: 1, code: "MATCH-1", shortName: "Match", fullName: "Match Full", state: PositionUnitState.Vacant, isActived: false);
        var occupiedMatch = NewPosition(number: 2, code: "MATCH-2", shortName: "Match", fullName: "Match Full", state: PositionUnitState.Occupied, isActived: true);
        var ok = NewPosition(number: 3, code: "MATCH-3", shortName: "Match", fullName: "Match Full", state: PositionUnitState.Vacant, isActived: true);

        await SeedAsync(inactiveMatch, occupiedMatch, ok);

        // Act
        var result = await _repo.GetVacantOptionsAsync(search: "MATCH", take: 50, ct: CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(ok.Id, result[0].Id);
    }
}
