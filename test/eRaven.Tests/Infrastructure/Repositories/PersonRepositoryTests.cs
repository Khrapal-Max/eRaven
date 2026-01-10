//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonRepositoryTests (SqliteTestDb + unique PositionUnit.Code)
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Domain.ValueObjects;
using eRaven.Infrastructure.Projectors;
using eRaven.Infrastructure.Repositories.PersonRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class PersonRepositoryTests : IAsyncLifetime
{
    private SqliteTestDb _db = default!;
    private IPersonReadModelProjector _projector = default!;
    private PersonRepository _repo = default!;

    private static readonly DateTime NowUtc = new(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc);

    public Task InitializeAsync()
    {
        _db = new SqliteTestDb();
        _projector = new PersonReadModelProjector();
        _repo = new PersonRepository(_db.Factory, _projector);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
        => await _db.DisposeAsync();

    // =========================================================
    // helpers
    // =========================================================

    private static PersonalInfo Personal(
        string rnokpp = "1234567890",
        string last = "Ivanov",
        string first = "Ivan",
        string? middle = null)
        => new(rnokpp, last, first, middle);

    /// <summary>
    /// IMPORTANT: SQLite has UNIQUE(position_units.code) => Code MUST be unique in tests.
    /// </summary>
    private static PositionUnit NewPos(Guid id, PositionUnitState state, int number)
        => new()
        {
            Id = id,
            Number = number,
            Code = $"PU-{number}-{id.ToString("N")[..8]}", // ✅ unique
            ShortName = $"S{number}",
            FullName = $"Unit/Pos/{number}",
            SpecialNumber = $"SP-{number}",
            State = state,
            Rank = $"R-{number}",
            Tarif = $"T-{number}",
            IsActived = true
        };

    private async Task SeedPositionsAsync(params PositionUnit[] positions)
    {
        await using var ctx = _db.Factory.CreateDbContext();
        ctx.PositionUnits.AddRange(positions);
        await ctx.SaveChangesAsync();
    }

    private async Task<PositionUnit> LoadPosAsync(Guid id)
    {
        await using var ctx = _db.Factory.CreateDbContext();
        return await ctx.PositionUnits.AsNoTracking().SingleAsync(x => x.Id == id);
    }

    private async Task<PersonReadModel?> LoadReadAsync(Guid personId)
    {
        await using var ctx = _db.Factory.CreateDbContext();
        return await ctx.PersonRead.AsNoTracking().SingleOrDefaultAsync(x => x.Id == personId);
    }

    // =========================================================
    // tests
    // =========================================================

    [Fact]
    public async Task SaveAsync_PositionChanged_should_vacate_old_main_and_occupy_new_main_and_update_read_model()
    {
        var personId = Guid.NewGuid();
        var main1Id = Guid.NewGuid();
        var main2Id = Guid.NewGuid();

        // ✅ unique Number => unique Code too
        await SeedPositionsAsync(
            NewPos(main1Id, PositionUnitState.Vacant, number: 1),
            NewPos(main2Id, PositionUnitState.Vacant, number: 2));

        // 1) Candidate reserves main1 (Vacant -> TemporarilyCandidate)
        var agg = PersonAggregate.CreateCandidate(
            id: personId,
            personal: Personal(),
            plannedPosition: "P",
            plannedPositionUnitId: main1Id,
            author: "tester",
            nowUtc: NowUtc);

        await _repo.SaveAsync(agg, expectedVersion: 0);

        Assert.Equal(PositionUnitState.TemporarilyCandidate, (await LoadPosAsync(main1Id)).State);
        Assert.Equal(PositionUnitState.Vacant, (await LoadPosAsync(main2Id)).State);

        // 2) Enroll on main1 => main1: TemporarilyCandidate -> Occupied
        agg.ChangeRank(
            effectiveDate: new DateOnly(2026, 01, 10),
            rank: "Сержант",
            note: null,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(1));

        agg.Enroll(
            kind: EnrollmentKind.Unit,
            reference: null,
            reason: "ok",
            enrollDate: new DateOnly(2026, 01, 10),
            PositionUnitId: main1Id,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(2));

        await _repo.SaveAsync(agg, expectedVersion: 1); // v1 was candidate create

        Assert.Equal(PositionUnitState.Occupied, (await LoadPosAsync(main1Id)).State);
        Assert.Equal(PositionUnitState.Vacant, (await LoadPosAsync(main2Id)).State);

        // 3) Change main position to main2:
        //    old main1: Occupied -> Vacant
        //    new main2: Vacant -> Occupied
        agg.ChangePosition(
            effectiveDate: new DateOnly(2026, 01, 11),
            positionUnitId: main2Id,
            position: "Командир відділення",
            note: null,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(3));

        await _repo.SaveAsync(agg, expectedVersion: 3);

        Assert.Equal(PositionUnitState.Vacant, (await LoadPosAsync(main1Id)).State);
        Assert.Equal(PositionUnitState.Occupied, (await LoadPosAsync(main2Id)).State);

        var rm = await LoadReadAsync(personId);
        Assert.NotNull(rm);

        Assert.Equal(PersonLifecycle.Enrolled, rm!.Lifecycle);
        Assert.Equal(main2Id, rm.PositionUnitId);
        Assert.Equal("Командир відділення", rm.Position);
        Assert.Equal(4, rm.Version);
    }
}
