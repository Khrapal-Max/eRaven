//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonRepositoryTests (SQLite in-memory + projector + event store) - updated for PositionSort
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PersonInfo;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.DTOs.Excel;
using eRaven.Application.Queries.Personal;
using eRaven.Domain.Enums;
using eRaven.Domain.Events.PersonEvents.Info;
using eRaven.Domain.Events.PersonEvents.Move;
using eRaven.Exceptions;
using eRaven.Infrastructure.Projectors;
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

    private static CreateReservedCommand CreateCmd(
        Guid id,
        string rnokpp,
        string last = "Ivanov",
        string first = "Ivan",
        string? middle = null,
        string? rank = null,
        string? position = null)
        => new(
            PersonId: id,
            Rnokpp: rnokpp,
            LastName: last,
            FirstName: first,
            MiddleName: middle,
            Rank: rank,
            PositionSort: null,
            Position: position,
            Author: "tester",
            NowUtc: NowUtc);

    private static UpdatePersonalInfoCommand UpdatePersonalCmd(Guid id, string rnokpp, string last, string first, string? middle = null, string? note = null)
        => new(
            PersonId: id,
            Rnokpp: rnokpp,
            LastName: last,
            FirstName: first,
            MiddleName: middle,
            Note: note,
            Author: "tester",
            NowUtc: NowUtc.AddMinutes(1));

    // =========================
    // tests
    // =========================

    [Fact]
    public async Task CreateReservedAsync_should_persist_event_and_create_read_model()
    {
        var id = Guid.NewGuid();

        await _repo.CreateReservedAsync(CreateCmd(
            id: id,
            rnokpp: "1234567890",
            middle: "Ivanovich",
            rank: " Сержант ",
            position: " Стрілець "));

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

        // normalized
        Assert.Equal("Сержант", rm.Rank);
        Assert.Equal("Стрілець", rm.Position);
        Assert.Null(rm.PositionSort); // ✅ CreateReserved не ставить сорт
        Assert.Null(rm.Bzvp);
        Assert.Null(rm.Weapon);
        Assert.Null(rm.Callsign);

        Assert.Null(rm.EnrolledAt);
        Assert.Null(rm.ExcludedAt);

        Assert.Equal(1, rm.Version);
        Assert.NotEqual(default, rm.UpdatedAtUtc);
    }

    [Fact]
    public async Task CreateReservedAsync_twice_should_throw_optimistic_concurrency()
    {
        var id = Guid.NewGuid();

        await _repo.CreateReservedAsync(CreateCmd(id, "1234567890"));

        var ex = await Assert.ThrowsAsync<OptimisticConcurrencyException>(() =>
            _repo.CreateReservedAsync(CreateCmd(id, "1234567890")));

        Assert.Equal(id, ex.AggregateId);
        Assert.Equal(0, ex.ExpectedVersion);
        Assert.True(ex.ActualVersion >= 1);
    }

    [Fact]
    public async Task UpdatePersonalInfoAsync_should_update_read_model_and_increment_version()
    {
        var id = Guid.NewGuid();

        await _repo.CreateReservedAsync(CreateCmd(id, "1234567890", last: "Ivanov", first: "Ivan"));
        await _repo.UpdatePersonalInfoAsync(UpdatePersonalCmd(id, "1234567890", last: "NEW", first: "NAME", middle: "MID"));

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
        var id = Guid.NewGuid();

        await _repo.CreateReservedAsync(CreateCmd(
            id, "1234567890",
            rank: "Солдат",
            position: "Стрілець"));

        await _repo.EnrollAsync(new EnrollCommand(
            PersonId: id,
            Kind: EnrollmentKind.Unit,
            Reference: "A",
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "Солдат",
            PositionSort: 10,
            Position: "Оператор",
            Author: "tester",
            NowUtc: NowUtc.AddMinutes(1)));

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
            Assert.Equal(10, dto.PositionSort);
        }

        await _repo.ExcludeAsync(new ExcludeCommand(
            PersonId: id,
            Reason: "x",
            EffectiveDate: new DateOnly(2026, 01, 20),
            Author: "tester",
            NowUtc: NowUtc.AddMinutes(2)));

        {
            var dto = await _repo.GetByIdAsync(id);
            Assert.NotNull(dto);
            Assert.Equal(PersonLifecycle.Reserved, dto!.Lifecycle);
            Assert.Equal(new DateOnly(2026, 01, 20), dto.ExcludedAt);

            // інші поля не чистимо
            Assert.Equal("Солдат", dto.Rank);
            Assert.Equal("Оператор", dto.Position);
            Assert.Equal(10, dto.PositionSort);

            Assert.Null(dto.EnrollmentKind);
            Assert.Null(dto.EnrollmentReference);
        }

        await _repo.EnrollAsync(new EnrollCommand(
            PersonId: id,
            Kind: EnrollmentKind.AttachedByOrder,
            Reference: "B",
            Reason: "re",
            EnrollDate: new DateOnly(2026, 06, 02),
            Rank: "Сержант",
            PositionSort: 9999,          // ✅ правило для приряджених
            Position: "Командир",
            Author: "tester",
            NowUtc: NowUtc.AddMinutes(3)));

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
        var id = Guid.NewGuid();

        await _repo.CreateReservedAsync(CreateCmd(id, "1234567890", rank: null, position: "P"));

        await _repo.ChangeRankAsync(new ChangeRankCommand(
            PersonId: id,
            EffectiveDate: new DateOnly(2026, 01, 05),
            Rank: "Солдат",
            Note: null,
            Author: "tester",
            NowUtc: NowUtc.AddMinutes(1)));

        // sanity: rank applied
        {
            var dto = await _repo.GetByIdAsync(id);
            Assert.NotNull(dto);
            Assert.Equal("Солдат", dto!.Rank);
        }

        // find rank event id from event store
        Guid rankEventId;
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var rankRec = await ctx.PersonEvents.AsNoTracking()
                .Where(x => x.AggregateId == id && x.EventType == nameof(PersonRankChanged))
                .OrderByDescending(x => x.Version)
                .FirstAsync();

            rankEventId = rankRec.EventId;
        }

        await _repo.VoidEventAsync(new VoidPersonEventCommand(
            PersonId: id,
            TargetEventId: rankEventId,
            Reason: "fix",
            Author: "tester",
            NowUtc: NowUtc.AddMinutes(2)));

        // after void => rebuild => rank removed
        {
            var dto = await _repo.GetByIdAsync(id);
            Assert.NotNull(dto);
            Assert.Null(dto!.Rank);

            // last version should include void
            Assert.True(dto.Version >= 3);
        }
    }

    [Fact]
    public async Task GetPageAsync_should_support_search_filters_and_asofdate()
    {
        // p1 active only in Jan 2026
        var p1 = Guid.NewGuid();
        await _repo.CreateReservedAsync(CreateCmd(p1, "1234567890", last: "Alpha", first: "A", rank: "R", position: "P"));
        await _repo.EnrollAsync(new EnrollCommand(
            PersonId: p1,
            Kind: EnrollmentKind.Unit,
            Reference: null,
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "soldier",
            PositionSort: 10,
            Position: "Pos1",
            Author: "tester",
            NowUtc: NowUtc.AddMinutes(1)));
        await _repo.ExcludeAsync(new ExcludeCommand(p1, "x", new DateOnly(2026, 01, 20), "tester", NowUtc.AddMinutes(2)));

        // p2 enrolled and still active
        var p2 = Guid.NewGuid();
        await _repo.CreateReservedAsync(CreateCmd(p2, "1234567891", last: "Bravo", first: "B", rank: "R", position: "P"));
        await _repo.EnrollAsync(new EnrollCommand(
            PersonId: p2,
            Kind: EnrollmentKind.AttachedByList,
            Reference: "REF",
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 05),
            Rank: "soldier",
            PositionSort: 9999,
            Position: "Pos2",
            Author: "tester",
            NowUtc: NowUtc.AddMinutes(1)));

        // p3 reserved only
        var p3 = Guid.NewGuid();
        await _repo.CreateReservedAsync(CreateCmd(p3, "1234567892", last: "Charlie", first: "C", rank: "R", position: "P"));

        // search by rnokpp fragment
        {
            var page = await _repo.GetPageAsync(new GetPersonsPageQuery(Page: 1, PageSize: 50, Search: "7891"));
            Assert.Single(page.Items);
            Assert.Equal(p2, page.Items[0].Id);
        }

        // filter by lifecycle Reserved
        {
            var page = await _repo.GetPageAsync(new GetPersonsPageQuery(Page: 1, PageSize: 50, Lifecycle: PersonLifecycle.Reserved));
            var ids = page.Items.Select(x => x.Id).ToHashSet();

            Assert.Contains(p1, ids); // excluded => Reserved in this model
            Assert.Contains(p3, ids);
            Assert.DoesNotContain(p2, ids);
        }

        // filter by enrollment kind
        {
            var page = await _repo.GetPageAsync(new GetPersonsPageQuery(Page: 1, PageSize: 50, EnrollmentKind: EnrollmentKind.AttachedByList));
            Assert.Single(page.Items);
            Assert.Equal(p2, page.Items[0].Id);
        }

        // as-of-date: Jan 15 => p1 & p2 active
        {
            var page = await _repo.GetPageAsync(new GetPersonsPageQuery(Page: 1, PageSize: 50, AsOfDate: new DateOnly(2026, 01, 15)));
            var ids = page.Items.Select(x => x.Id).ToHashSet();
            Assert.Contains(p1, ids);
            Assert.Contains(p2, ids);
            Assert.DoesNotContain(p3, ids);
        }

        // as-of-date: Jan 25 => p1 not active, p2 active
        {
            var page = await _repo.GetPageAsync(new GetPersonsPageQuery(Page: 1, PageSize: 50, AsOfDate: new DateOnly(2026, 01, 25)));
            var ids = page.Items.Select(x => x.Id).ToHashSet();
            Assert.DoesNotContain(p1, ids);
            Assert.Contains(p2, ids);
            Assert.DoesNotContain(p3, ids);
        }
    }

    [Fact]
    public async Task GetHistoryAsync_should_return_ordered_versions_and_event_types()
    {
        var id = Guid.NewGuid();

        await _repo.CreateReservedAsync(CreateCmd(id, "1234567890", last: "Ivanov", first: "Ivan"));

        await _repo.ChangePositionAsync(new ChangePositionCommand(
            PersonId: id,
            EffectiveDate: new DateOnly(2026, 01, 03),
            PositionSort: 20,
            Position: "O",
            Note: null,
            Author: "tester",
            NowUtc: NowUtc.AddMinutes(1)));

        var history = await _repo.GetHistoryAsync(id);

        Assert.True(history.Count >= 2);
        Assert.Equal(1, history[0].Version);
        Assert.Equal(2, history[1].Version);

        Assert.Equal(nameof(PersonCreated), history[0].EventType);
        Assert.Equal(nameof(PersonPositionChanged), history[1].EventType);
        Assert.False(string.IsNullOrWhiteSpace(history[0].PayloadJson));
    }

    [Fact]
    public async Task EnrollAsync_when_created_without_rank_should_set_rank_from_command()
    {
        var id = Guid.NewGuid();

        // rank відсутній на створенні
        await _repo.CreateReservedAsync(CreateCmd(
            id: id,
            rnokpp: "1234567890",
            rank: null,
            position: "P"));

        await _repo.EnrollAsync(new EnrollCommand(
            PersonId: id,
            Kind: EnrollmentKind.Unit,
            Reference: "A",
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "Солдат",
            PositionSort: 10,
            Position: "Оператор",
            Author: "tester",
            NowUtc: NowUtc.AddMinutes(1)));

        var dto = await _repo.GetByIdAsync(id);
        Assert.NotNull(dto);

        Assert.Equal(PersonLifecycle.Enrolled, dto!.Lifecycle);
        Assert.Equal("Солдат", dto.Rank);
        Assert.Equal("Оператор", dto.Position);
        Assert.Equal(10, dto.PositionSort);

        Assert.Equal(EnrollmentKind.Unit, dto.EnrollmentKind);
        Assert.Equal("A", dto.EnrollmentReference);
    }

    // =========================
    // GetExistingRnokppsAsync
    // =========================

    [Fact]
    public async Task GetExistingRnokppsAsync_should_return_only_existing_trimmed_and_distinct_values()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        await _repo.CreateReservedAsync(new CreateReservedCommand(
            PersonId: id1,
            Rnokpp: "1234567890",
            LastName: "Ivanov",
            FirstName: "Ivan",
            MiddleName: null,
            Rank: null,
            PositionSort: null,
            Position: null,
            Author: "tester",
            NowUtc: NowUtc));

        await _repo.CreateReservedAsync(new CreateReservedCommand(
            PersonId: id2,
            Rnokpp: "0987654321",
            LastName: "Petrov",
            FirstName: "Petr",
            MiddleName: null,
            Rank: null,
            PositionSort: null,
            Position: null,
            Author: "tester",
            NowUtc: NowUtc));

        var input = new[]
        {
            " 1234567890 ",
            "0987654321",
            "0000000000",   // not existing
            "0987654321",   // duplicate
            "",             // ignored
            "   "           // ignored
        };

        var existing = await _repo.GetExistingRnokppsAsync(input);

        Assert.Equal(2, existing.Count);
        Assert.Contains("1234567890", existing);
        Assert.Contains("0987654321", existing);
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

    // =========================
    // BootstrapCreateAndEnrollAsync
    // =========================

    [Fact]
    public async Task BootstrapCreateAndEnrollAsync_unit_should_create_enrolled_person_with_optional_fields_and_effective_dates()
    {
        var enrollDate = new DateOnly(2026, 01, 10);

        var row = new PersonBootstrapRowDto(
            RowNumber: 1,
            Rnokpp: "1234567890",
            LastName: "  Ivanov ",
            FirstName: " Ivan ",
            MiddleName: "  Ivanovich ",
            Kind: EnrollmentKind.Unit,
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

        Assert.Equal("1234567890", dto.Rnokpp);
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
            Rnokpp: "1234567890",
            LastName: "Ivanov",
            FirstName: "Ivan",
            MiddleName: null,
            Kind: EnrollmentKind.AttachedByOrder,
            Reference: "X",
            EnrollDate: new DateOnly(2026, 02, 01),
            Reason: "r",
            Rank: "S",
            PositionSort: 1,   // should be ignored
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
            Rnokpp: "1234567890",
            LastName: "Ivanov",
            FirstName: "Ivan",
            MiddleName: null,
            Kind: EnrollmentKind.Unit,
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
