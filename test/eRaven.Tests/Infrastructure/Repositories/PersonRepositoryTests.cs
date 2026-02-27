//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonRepositoryTests (SQLite in-memory + projector + event store)
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Excel;
using eRaven.Domain.Enums;
using eRaven.Domain.Events.PersonEvents.Info;
using eRaven.Domain.Events.PersonEvents.Move;
using eRaven.Infrastructure.Projections.Person;
using eRaven.Infrastructure.Repositories.PersonRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class PersonRepositoryTests : IAsyncLifetime
{
    private SqliteTestDb _db = default!;
    private PersonRepository _repo = default!;
    private PersonReadModelProjector _projector = default!;

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

    // =========================
    // helpers
    // =========================

    private Task<Guid> CreateReservedAsync(
        string rnokpp = "1234567890",
        string last = "Ivanov",
        string first = "Ivan",
        string? middle = "Ivanovich",
        string? rank = "Сержант",
        string? position = "Стрілець")
        => _repo.CreateReservedAsync(
            rnokpp: rnokpp,
            lastName: last,
            firstName: first,
            middleName: middle,
            rank: rank,
            position: position,
            author: "tester",
            nowUtc: NowUtc);

    // =========================
    // tests
    // =========================

    [Fact]
    public async Task CreateReservedAsync_should_persist_event_and_create_read_model()
    {
        var id = await CreateReservedAsync();

        await using var ctx = _db.Factory.CreateDbContext();

        var eventsCount = await ctx.PersonEvents.CountAsync(x => x.AggregateId == id);
        Assert.Equal(1, eventsCount);

        var rm = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

        Assert.Equal(PersonLifecycle.Reserved, rm.Lifecycle);
        Assert.Null(rm.EnrollmentKind);
        Assert.Null(rm.EnrollmentReference);

        Assert.Equal("1234567890", rm.Rnokpp);
        Assert.Equal("Ivanov", rm.LastName);
        Assert.Equal("Ivan", rm.FirstName);
        Assert.Equal("Ivanovich", rm.MiddleName);
        Assert.Equal("Ivanov Ivan Ivanovich", rm.FullName);

        Assert.Equal("Сержант", rm.Rank);
        Assert.Equal("Стрілець", rm.Position);

        // CreateReserved не ставить sort (у вас він nullable)
        Assert.Null(rm.PositionSort);

        Assert.Null(rm.Bzvp);
        Assert.Null(rm.Weapon);
        Assert.Null(rm.Callsign);

        Assert.Null(rm.EnrolledAt);
        Assert.Null(rm.ExcludedAt);

        Assert.Equal(1, rm.Version);
        Assert.NotEqual(default, rm.UpdatedAtUtc);
    }

    [Fact]
    public async Task CreateReservedAsync_same_rnokpp_should_throw_unique_constraint()
    {
        await CreateReservedAsync(rnokpp: "9999999999");

        await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
            CreateReservedAsync(rnokpp: "9999999999"));

        await using var ctx = _db.Factory.CreateDbContext();

        // sanity: в read-моделі рівно 1 запис з цим RNOKPP
        var cnt = await ctx.PersonRead.CountAsync(x => x.Rnokpp == "9999999999");
        Assert.Equal(1, cnt);
    }

    [Fact]
    public async Task UpdatePersonalInfoAsync_should_update_read_model_and_increment_version()
    {
        var id = await CreateReservedAsync(
            rnokpp: "1111111111",
            last: "Ivanov",
            first: "Ivan",
            middle: null,
            rank: null,
            position: "P");

        await _repo.UpdatePersonalInfoAsync(
            personId: id,
            rnokpp: "1111111111",
            lastName: "NEW",
            firstName: "NAME",
            middleName: "MID",
            Note: null,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(1));

        var dto = await _repo.GetByIdAsync(id);
        Assert.NotNull(dto);

        Assert.Equal("NEW", dto!.LastName);
        Assert.Equal("NAME", dto.FirstName);
        Assert.Equal("MID", dto.MiddleName);
        Assert.Equal("NEW NAME MID", dto.FullName);

        Assert.Equal(2, dto.Version);

        var history = await _repo.GetHistoryAsync(id);
        Assert.Equal(2, history.Count);
        Assert.Equal(1, history[0].Version);
        Assert.Equal(2, history[1].Version);
    }

    [Fact]
    public async Task Enroll_Exclude_ReEnroll_flow_should_update_read_model_as_expected()
    {
        var id = await CreateReservedAsync(
            rnokpp: "2222222222",
            rank: "Солдат",
            position: "Стрілець");

        await _repo.EnrollAsync(
            personId: id,
            kind: EnrollmentKind.Unit,
            reference: "A",
            reason: "r",
            enrollDate: new DateOnly(2026, 01, 10),
            rank: "Солдат",
            positionSort: 10,
            position: "Оператор",
            author: "tester",
            nowUtc: NowUtc.AddMinutes(1));

        {
            var dto = await _repo.GetByIdAsync(id);
            Assert.NotNull(dto);

            Assert.Equal(PersonLifecycle.Enrolled, dto!.Lifecycle);
            Assert.Equal(EnrollmentKind.Unit, dto.EnrollmentKind);
            Assert.Equal("A", dto.EnrollmentReference);
            Assert.Equal(new DateOnly(2026, 01, 10), dto.EnrolledAt);
            Assert.Null(dto.ExcludedAt);

            Assert.Equal("Солдат", dto.Rank);
            Assert.Equal(10, dto.PositionSort);
            Assert.Equal("Оператор", dto.Position);
        }

        await _repo.ExcludeAsync(
            personId: id,
            reason: "x",
            effectiveDate: new DateOnly(2026, 01, 20),
            author: "tester",
            nowUtc: NowUtc.AddMinutes(2));

        {
            var dto = await _repo.GetByIdAsync(id);
            Assert.NotNull(dto);

            Assert.Equal(PersonLifecycle.Reserved, dto!.Lifecycle);
            Assert.Equal(new DateOnly(2026, 01, 20), dto.ExcludedAt);

            // поля картки не чистимо
            Assert.Equal("Солдат", dto.Rank);
            Assert.Equal("Оператор", dto.Position);
            Assert.Equal(10, dto.PositionSort);

            Assert.Null(dto.EnrollmentKind);
            Assert.Null(dto.EnrollmentReference);
        }

        await _repo.EnrollAsync(
            personId: id,
            kind: EnrollmentKind.AttachedByOrder,
            reference: "B",
            reason: "re",
            enrollDate: new DateOnly(2026, 06, 02),
            rank: "Сержант",
            positionSort: 9999,        // правило для приряджених
            position: "Командир",
            author: "tester",
            nowUtc: NowUtc.AddMinutes(3));

        {
            var dto = await _repo.GetByIdAsync(id);
            Assert.NotNull(dto);

            Assert.Equal(PersonLifecycle.Enrolled, dto!.Lifecycle);
            Assert.Equal(EnrollmentKind.AttachedByOrder, dto.EnrollmentKind);
            Assert.Equal("B", dto.EnrollmentReference);
            Assert.Equal(new DateOnly(2026, 06, 02), dto.EnrolledAt);
            Assert.Null(dto.ExcludedAt);

            Assert.Equal("Сержант", dto.Rank);
            Assert.Equal("Командир", dto.Position);
            Assert.Equal(9999, dto.PositionSort);
        }
    }

    [Fact]
    public async Task VoidEventAsync_should_rebuild_and_remove_voided_effect()
    {
        var id = await CreateReservedAsync(
            rnokpp: "3333333333",
            rank: null,
            position: "P");

        await _repo.ChangeRankAsync(
            personId: id,
            effectiveDate: new DateOnly(2026, 01, 05),
            rank: "Солдат",
            note: null,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(1));

        // sanity
        {
            var dto = await _repo.GetByIdAsync(id);
            Assert.NotNull(dto);
            Assert.Equal("Солдат", dto!.Rank);
        }

        // знайти eventId для RankChanged
        Guid rankEventId;
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var rankRec = await ctx.PersonEvents.AsNoTracking()
                .Where(x => x.AggregateId == id && x.EventType == nameof(PersonRankChanged))
                .OrderByDescending(x => x.Version)
                .FirstAsync();

            rankEventId = rankRec.EventId;
        }

        await _repo.VoidEventAsync(
            personId: id,
            targetEventId: rankEventId,
            reason: "fix",
            author: "tester",
            nowUtc: NowUtc.AddMinutes(2));

        // after void => rebuild => rank removed
        {
            var dto = await _repo.GetByIdAsync(id);
            Assert.NotNull(dto);
            Assert.Null(dto!.Rank);
            Assert.True(dto.Version >= 3);
        }
    }

    [Fact]
    public async Task GetPageAsync_should_support_search_filters_and_asofdate()
    {
        // p1 active only in Jan 2026
        var p1 = await CreateReservedAsync(rnokpp: "4444444444", last: "Alpha", first: "A", rank: "R", position: "P");
        await _repo.EnrollAsync(
            personId: p1,
            kind: EnrollmentKind.Unit,
            reference: null,
            reason: "r",
            enrollDate: new DateOnly(2026, 01, 10),
            rank: "soldier",
            positionSort: 10,
            position: "Pos1",
            author: "tester",
            nowUtc: NowUtc.AddMinutes(1));
        await _repo.ExcludeAsync(
            personId: p1,
            reason: "x",
            effectiveDate: new DateOnly(2026, 01, 20),
            author: "tester",
            nowUtc: NowUtc.AddMinutes(2));

        // p2 enrolled and still active
        var p2 = await CreateReservedAsync(rnokpp: "5555555555", last: "Bravo", first: "B", rank: "R", position: "P");
        await _repo.EnrollAsync(
            personId: p2,
            kind: EnrollmentKind.AttachedByList,
            reference: "REF",
            reason: "r",
            enrollDate: new DateOnly(2026, 01, 05),
            rank: "soldier",
            positionSort: 9999,
            position: "Pos2",
            author: "tester",
            nowUtc: NowUtc.AddMinutes(3));

        // p3 reserved only
        var p3 = await CreateReservedAsync(rnokpp: "6666666666", last: "Charlie", first: "C", rank: "R", position: "P");

        // search by rnokpp fragment
        {
            var page = await _repo.GetPageAsync(page: 1, pageSize: 50, search: "5555");
            Assert.Single(page.Items);
            Assert.Equal(p2, page.Items[0].Id);
        }

        // filter by lifecycle Reserved
        {
            var page = await _repo.GetPageAsync(page: 1, pageSize: 50, lifecycle: PersonLifecycle.Reserved);
            var ids = page.Items.Select(x => x.Id).ToHashSet();

            Assert.Contains(p1, ids); // excluded => Reserved
            Assert.Contains(p3, ids);
            Assert.DoesNotContain(p2, ids);
        }

        // filter by enrollment kind
        {
            var page = await _repo.GetPageAsync(page: 1, pageSize: 50, enrollmentKind: EnrollmentKind.AttachedByList);
            Assert.Single(page.Items);
            Assert.Equal(p2, page.Items[0].Id);
        }

        // as-of-date: Jan 15 => p1 & p2 active
        {
            var page = await _repo.GetPageAsync(page: 1, pageSize: 50, asOfDate: new DateOnly(2026, 01, 15));
            var ids = page.Items.Select(x => x.Id).ToHashSet();

            Assert.Contains(p1, ids);
            Assert.Contains(p2, ids);
            Assert.DoesNotContain(p3, ids);
        }

        // as-of-date: Jan 25 => p1 not active, p2 active
        {
            var page = await _repo.GetPageAsync(page: 1, pageSize: 50, asOfDate: new DateOnly(2026, 01, 25));
            var ids = page.Items.Select(x => x.Id).ToHashSet();

            Assert.DoesNotContain(p1, ids);
            Assert.Contains(p2, ids);
            Assert.DoesNotContain(p3, ids);
        }
    }

    [Fact]
    public async Task GetHistoryAsync_should_return_ordered_versions_and_event_types()
    {
        var id = await CreateReservedAsync(rnokpp: "7777777777", last: "Ivanov", first: "Ivan");

        await _repo.ChangePositionAsync(
            personId: id,
            effectiveDate: new DateOnly(2026, 01, 03),
            positionSort: 20,
            position: "O",
            note: null,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(1));

        var history = await _repo.GetHistoryAsync(id);

        Assert.True(history.Count >= 2);
        Assert.Equal(1, history[0].Version);
        Assert.Equal(2, history[1].Version);

        Assert.Equal(nameof(PersonCreated), history[0].EventType);
        Assert.Equal(nameof(PersonPositionChanged), history[1].EventType);
        Assert.False(string.IsNullOrWhiteSpace(history[0].PayloadJson));
    }

    [Fact]
    public async Task EnrollAsync_when_created_without_rank_should_set_rank_from_enroll_parameters()
    {
        var id = await CreateReservedAsync(
            rnokpp: "8888888888",
            rank: null,
            position: "P");

        await _repo.EnrollAsync(
            personId: id,
            kind: EnrollmentKind.Unit,
            reference: "A",
            reason: "r",
            enrollDate: new DateOnly(2026, 01, 10),
            rank: "Солдат",
            positionSort: 10,
            position: "Оператор",
            author: "tester",
            nowUtc: NowUtc.AddMinutes(1));

        var dto = await _repo.GetByIdAsync(id);
        Assert.NotNull(dto);

        Assert.Equal(PersonLifecycle.Enrolled, dto!.Lifecycle);
        Assert.Equal("Солдат", dto.Rank);
        Assert.Equal("Оператор", dto.Position);
        Assert.Equal(10, dto.PositionSort);

        Assert.Equal(EnrollmentKind.Unit, dto.EnrollmentKind);
        Assert.Equal("A", dto.EnrollmentReference);
    }

    [Fact]
    public async Task GetExistingRnokppsAsync_should_return_only_existing_trimmed_and_distinct_values()
    {
        await CreateReservedAsync(rnokpp: "9000000001");
        await CreateReservedAsync(rnokpp: "9000000002");

        var input = new[]
        {
            " 9000000001 ",
            "9000000002",
            "0000000000",   // not existing
            "9000000002",   // duplicate
            "",             // ignored
            "   "           // ignored
        };

        var existing = await _repo.GetExistingRnokppsAsync(input);

        Assert.Equal(2, existing.Count);
        Assert.Contains("9000000001", existing);
        Assert.Contains("9000000002", existing);
        Assert.DoesNotContain("0000000000", existing);
    }

    [Fact]
    public async Task GetExistingRnokppsAsync_when_input_is_null_or_empty_should_return_empty_set()
    {
        var empty1 = await _repo.GetExistingRnokppsAsync(null!);
        Assert.Empty(empty1);

        var empty2 = await _repo.GetExistingRnokppsAsync([]);
        Assert.Empty(empty2);
    }

    [Fact]
    public async Task BootstrapCreateAndEnrollAsync_unit_should_create_enrolled_person_with_optional_fields_and_effective_dates()
    {
        var enrollDate = new DateOnly(2026, 01, 10);

        var row = new PersonBootstrapRowDto(
            RowNumber: 1,
            Rnokpp: "9100000001",
            LastName: "  Ivanov ",
            FirstName: " Ivan ",
            MiddleName: "  Ivanovich ",
            Kind: EnrollmentKindDto.Unit,
            Reference: "  REF-1  ",
            EnrollDate: enrollDate,
            Reason: "  import  ",
            Rank: "  Soldier  ",
            PositionSort: 10,
            Position: "  Operator  ",
            Bzvp: "  BZVP  ",
            Weapon: "  AK  ",
            Callsign: "  Fox  ");

        var id = await _repo.BootstrapCreateAndEnrollAsync(row, author: "importer", nowUtc: NowUtc);

        var dto = await _repo.GetByIdAsync(id);
        Assert.NotNull(dto);

        Assert.Equal(PersonLifecycle.Enrolled, dto!.Lifecycle);
        Assert.Equal(EnrollmentKind.Unit, dto.EnrollmentKind);
        Assert.Equal("REF-1", dto.EnrollmentReference);
        Assert.Equal(enrollDate, dto.EnrolledAt);
        Assert.Null(dto.ExcludedAt);

        Assert.Equal("9100000001", dto.Rnokpp);
        Assert.Equal("Ivanov", dto.LastName);
        Assert.Equal("Ivan", dto.FirstName);
        Assert.Equal("Ivanovich", dto.MiddleName);
        Assert.Equal("Ivanov Ivan Ivanovich", dto.FullName);

        Assert.Equal("Soldier", dto.Rank);
        Assert.Equal(10, dto.PositionSort);
        Assert.Equal("Operator", dto.Position);

        Assert.Equal("BZVP", dto.Bzvp);
        Assert.Equal("AK", dto.Weapon);
        Assert.Equal("Fox", dto.Callsign);

        // events + effective dates in event store
        await using var ctx = _db.Factory.CreateDbContext();
        var events = await ctx.PersonEvents
            .AsNoTracking()
            .Where(x => x.AggregateId == id)
            .OrderBy(x => x.Version)
            .ToListAsync();

        Assert.Equal(5, events.Count);

        Assert.Equal(nameof(PersonCreated), events[0].EventType);
        Assert.Null(events[0].EffectiveDate);

        Assert.Equal(nameof(PersonEnrolled), events[1].EventType);
        Assert.Equal(enrollDate, events[1].EffectiveDate);

        Assert.Equal(nameof(PersonBzvpChanged), events[2].EventType);
        Assert.Equal(enrollDate, events[2].EffectiveDate);

        Assert.Equal(nameof(PersonWeaponChanged), events[3].EventType);
        Assert.Equal(enrollDate, events[3].EffectiveDate);

        Assert.Equal(nameof(PersonCallsignChanged), events[4].EventType);
        Assert.Equal(enrollDate, events[4].EffectiveDate);
    }

    [Fact]
    public async Task BootstrapCreateAndEnrollAsync_non_unit_should_force_position_sort_9999()
    {
        var row = new PersonBootstrapRowDto(
            RowNumber: 2,
            Rnokpp: "9100000002",
            LastName: "Ivanov",
            FirstName: "Ivan",
            MiddleName: null,
            Kind: EnrollmentKindDto.AttachedByOrder,
            Reference: "X",
            EnrollDate: new DateOnly(2026, 02, 01),
            Reason: "r",
            Rank: "S",
            PositionSort: 1,   // should be ignored by rule
            Position: "P",
            Bzvp: null,
            Weapon: null,
            Callsign: null);

        var id = await _repo.BootstrapCreateAndEnrollAsync(row, author: "importer", nowUtc: NowUtc);

        var dto = await _repo.GetByIdAsync(id);
        Assert.NotNull(dto);

        Assert.Equal(PersonLifecycle.Enrolled, dto!.Lifecycle);
        Assert.Equal(EnrollmentKind.AttachedByOrder, dto.EnrollmentKind);
        Assert.Equal(9999, dto.PositionSort);
    }

    [Fact]
    public async Task BootstrapCreateAndEnrollAsync_should_throw_when_required_fields_are_missing()
    {
        var bad = new PersonBootstrapRowDto(
            RowNumber: 3,
            Rnokpp: "9100000003",
            LastName: "Ivanov",
            FirstName: "Ivan",
            MiddleName: null,
            Kind: EnrollmentKindDto.Unit,
            Reference: null,
            EnrollDate: new DateOnly(2026, 01, 10),
            Reason: "r",
            Rank: " ", // required
            PositionSort: 10,
            Position: "P",
            Bzvp: null,
            Weapon: null,
            Callsign: null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _repo.BootstrapCreateAndEnrollAsync(bad, author: "importer", nowUtc: NowUtc));
    }
}
