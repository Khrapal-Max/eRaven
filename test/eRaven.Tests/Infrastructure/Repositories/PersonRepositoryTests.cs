//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonRepositoryTests (SqliteTestDb + unique PositionUnit.Code)
//-----------------------------------------------------------------------------

using eRaven.Application.Queries;
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

    [Fact]
    public async Task GetPersonsPageAsync_should_return_candidate_with_planned_position_in_dto()
    {
        var personId = Guid.NewGuid();
        var plannedPosId = Guid.NewGuid();

        await SeedPositionsAsync(
            NewPos(plannedPosId, PositionUnitState.Vacant, number: 10));

        var agg = PersonAggregate.CreateCandidate(
            id: personId,
            personal: Personal(rnokpp: "1111111111"),
            plannedPosition: "Резерв-на-посаду",
            plannedPositionUnitId: plannedPosId,
            author: "tester",
            nowUtc: NowUtc);

        await _repo.SaveAsync(agg, expectedVersion: 0);

        var page = await _repo.GetPersonsPageAsync(new GetPersonsPageQuery(
            Page: 1,
            PageSize: 50,
            Search: null,
            AsOfDate: null,
            Lifecycle: PersonLifecycle.Candidate,
            EnrollmentKind: null
        ));

        Assert.Single(page.Items);

        var row = page.Items[0];
        Assert.Equal(personId, row.Id);
        Assert.Equal(PersonLifecycle.Candidate, row.Lifecycle);

        // 🔥 це ключове: якщо тут null/порожньо — проблема в projector або мапінгу
        Assert.Equal("Резерв-на-посаду", row.PlannedPosition);

        // кандидат не має мати фактичних посад
        Assert.True(string.IsNullOrWhiteSpace(row.Position));
        Assert.True(string.IsNullOrWhiteSpace(row.TemporaryPosition));
    }

    [Fact]
    public async Task GetPersonsPageAsync_should_sort_by_updatedAt_desc_and_page_correctly()
    {
        var pos1 = Guid.NewGuid();
        var pos2 = Guid.NewGuid();

        await SeedPositionsAsync(
            NewPos(pos1, PositionUnitState.Vacant, number: 21),
            NewPos(pos2, PositionUnitState.Vacant, number: 22));

        // older
        var p1 = Guid.NewGuid();
        var a1 = PersonAggregate.CreateCandidate(
            id: p1,
            personal: Personal(rnokpp: "2222222222", last: "A", first: "A"),
            plannedPosition: "P1",
            plannedPositionUnitId: pos1,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(1));
        await _repo.SaveAsync(a1, expectedVersion: 0);

        // newer
        var p2 = Guid.NewGuid();
        var a2 = PersonAggregate.CreateCandidate(
            id: p2,
            personal: Personal(rnokpp: "3333333333", last: "B", first: "B"),
            plannedPosition: "P2",
            plannedPositionUnitId: pos2,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(2));
        await _repo.SaveAsync(a2, expectedVersion: 0);

        // page size 1 => має прийти “новіший” першим
        var page1 = await _repo.GetPersonsPageAsync(new GetPersonsPageQuery(Page: 1, PageSize: 1));
        Assert.Single(page1.Items);
        Assert.Equal(p2, page1.Items[0].Id);

        var page2 = await _repo.GetPersonsPageAsync(new GetPersonsPageQuery(Page: 2, PageSize: 1));
        Assert.Single(page2.Items);
        Assert.Equal(p1, page2.Items[0].Id);

        Assert.Equal(2, page1.TotalCount);
        Assert.Equal(2, page2.TotalCount);
    }

    [Fact]
    public async Task GetPersonsPageAsync_should_filter_by_lifecycle_and_enrollmentKind()
    {
        var plannedPos = Guid.NewGuid();
        var mainPos = Guid.NewGuid();

        await SeedPositionsAsync(
            NewPos(plannedPos, PositionUnitState.Vacant, number: 31),
            NewPos(mainPos, PositionUnitState.Vacant, number: 32));

        // Candidate
        var candId = Guid.NewGuid();
        var cand = PersonAggregate.CreateCandidate(
            id: candId,
            personal: Personal(rnokpp: "4444444444"),
            plannedPosition: "Reserve",
            plannedPositionUnitId: plannedPos,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(1));
        await _repo.SaveAsync(cand, expectedVersion: 0); // v1

        // Enrolled (EnrollmentKind.Unit)
        var enrId = Guid.NewGuid();
        var enr = PersonAggregate.CreateCandidate(
            id: enrId,
            personal: Personal(rnokpp: "5555555555"),
            plannedPosition: "Reserve2",
            plannedPositionUnitId: mainPos,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(2));
        await _repo.SaveAsync(enr, expectedVersion: 0); // v1

        // Rank is required BEFORE Enroll
        enr.ChangeRank(
            effectiveDate: new DateOnly(2026, 01, 10),
            rank: "Солдат",
            note: null,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(3));               // v2 (uncommitted)

        enr.Enroll(
            kind: EnrollmentKind.Unit,
            reference: null,
            reason: "ok",
            enrollDate: new DateOnly(2026, 01, 10),
            PositionUnitId: mainPos,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(4));               // v3 (uncommitted)

        await _repo.SaveAsync(enr, expectedVersion: 1);  // current version in DB is 1 (candidate created)

        // Filter Candidate only
        var candPage = await _repo.GetPersonsPageAsync(new GetPersonsPageQuery(
            Page: 1,
            PageSize: 50,
            Lifecycle: PersonLifecycle.Candidate
        ));

        Assert.Single(candPage.Items);
        Assert.Equal(candId, candPage.Items[0].Id);

        // Filter Enrolled + EnrollmentKind.Unit
        var enrPage = await _repo.GetPersonsPageAsync(new GetPersonsPageQuery(
            Page: 1,
            PageSize: 50,
            Lifecycle: PersonLifecycle.Enrolled,
            EnrollmentKind: EnrollmentKind.Unit
        ));

        Assert.Single(enrPage.Items);
        Assert.Equal(enrId, enrPage.Items[0].Id);
        Assert.Equal(EnrollmentKind.Unit, enrPage.Items[0].EnrollmentKind);
    }

    [Fact]
    public async Task GetPersonsPageAsync_AsOfDate_should_exclude_person_if_excluded_on_or_before_date()
    {
        var mainPos = Guid.NewGuid();

        await SeedPositionsAsync(
            NewPos(mainPos, PositionUnitState.Vacant, number: 41));

        var personId = Guid.NewGuid();
        var agg = PersonAggregate.CreateCandidate(
            id: personId,
            personal: Personal(rnokpp: "6666666666"),
            plannedPosition: "Reserve",
            plannedPositionUnitId: mainPos,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(1));

        await _repo.SaveAsync(agg, expectedVersion: 0); // v1

        // Rank is required BEFORE Enroll
        agg.ChangeRank(
            effectiveDate: new DateOnly(2026, 01, 10),
            rank: "Солдат",
            note: null,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(2));              // v2

        agg.Enroll(
            kind: EnrollmentKind.Unit,
            reference: null,
            reason: "ok",
            enrollDate: new DateOnly(2026, 01, 10),
            PositionUnitId: mainPos,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(3));              // v3

        await _repo.SaveAsync(agg, expectedVersion: 1); // current version is 1 after candidate created

        agg.Exclude(
            effectiveDate: new DateOnly(2026, 01, 12),
            reason: "done",
            author: "tester",
            nowUtc: NowUtc.AddMinutes(4));              // v4

        await _repo.SaveAsync(agg, expectedVersion: 3); // after rank+enroll saved, stream version is 3

        // as-of BEFORE excluded => ще має показуватися (за твоєю поточною логікою)
        var before = await _repo.GetPersonsPageAsync(new GetPersonsPageQuery(
            Page: 1,
            PageSize: 50,
            AsOfDate: new DateOnly(2026, 01, 11)
        ));
        Assert.Contains(before.Items, x => x.Id == personId);

        // as-of ON excluded date => має бути виключений
        var onDate = await _repo.GetPersonsPageAsync(new GetPersonsPageQuery(
            Page: 1,
            PageSize: 50,
            AsOfDate: new DateOnly(2026, 01, 12)
        ));
        Assert.DoesNotContain(onDate.Items, x => x.Id == personId);

        // as-of AFTER excluded => також не має бути
        var after = await _repo.GetPersonsPageAsync(new GetPersonsPageQuery(
            Page: 1,
            PageSize: 50,
            AsOfDate: new DateOnly(2026, 01, 13)
        ));
        Assert.DoesNotContain(after.Items, x => x.Id == personId);
    }
}
