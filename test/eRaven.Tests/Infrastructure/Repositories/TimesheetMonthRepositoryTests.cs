//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetMonthRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetMonthRepositoryTests
{
    private static readonly DateTime NowUtc = new(2026, 01, 23, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetTimesheetMonthAsync_builds_month_grid_with_nb_defaults_and_entries()
    {
        await using var tdb = new SqliteTestDb();

        using (var db = tdb.Factory.CreateDbContext())
        {
            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));
            var p2 = NewPerson("222", "Petrenko Petro", EnrollmentKind.Unit, new DateOnly(2026, 01, 10));
            var p3 = NewPerson("333", "Sydorenko Sydir", EnrollmentKind.Unit, new DateOnly(2026, 01, 01),
                excludedAt: new DateOnly(2026, 01, 15));

            db.PersonRead.AddRange(p1, p2, p3);

            var t1m = NewTimeline(p1.Id, TimesheetLane.Main, openedAt: new DateOnly(2026, 01, 01));
            var t1t = NewTimeline(p1.Id, TimesheetLane.Task, openedAt: new DateOnly(2026, 01, 01));

            var t2m = NewTimeline(p2.Id, TimesheetLane.Main, openedAt: new DateOnly(2026, 01, 10));
            var t2t = NewTimeline(p2.Id, TimesheetLane.Task, openedAt: new DateOnly(2026, 01, 10));

            var t3m = NewTimeline(p3.Id, TimesheetLane.Main, openedAt: new DateOnly(2026, 01, 01),
                closedAt: new DateOnly(2026, 01, 15));
            var t3t = NewTimeline(p3.Id, TimesheetLane.Task, openedAt: new DateOnly(2026, 01, 01),
                closedAt: new DateOnly(2026, 01, 15));

            db.TimesheetTimelines.AddRange(t1m, t1t, t2m, t2t, t3m, t3t);

            db.TimesheetEntries.AddRange(
                NewEntry(t1m, p1.Id, TimesheetLane.Main, "30", from: new DateOnly(2026, 01, 01), to: null),
                NewEntry(t2m, p2.Id, TimesheetLane.Main, "30", from: new DateOnly(2026, 01, 10), to: null),
                NewEntry(t3m, p3.Id, TimesheetLane.Main, "30", from: new DateOnly(2026, 01, 01), to: new DateOnly(2026, 01, 15))
            );

            db.SaveChanges();
        }

        var repo = new TimesheetMonthRepository(tdb.Factory);

        // act
        var rows = await repo.GetTimesheetMonthAsync(2026, 1, search: null);

        // assert
        var daysInMonth = DateTime.DaysInMonth(2026, 1);

        Assert.Equal(3, rows.Count);

        // усі рядки мають мати масиви під місяць
        Assert.All(rows, r =>
        {
            Assert.Equal(daysInMonth, r.MainCodes.Count);
            Assert.Equal(daysInMonth, r.TaskCodes.Count);

            // NEW: ref100 array must exist and match month length
            Assert.Equal(daysInMonth, r.MainRef.Count);

            // seed has no 100 => all null/empty
            Assert.All(r.MainRef, x => Assert.True(string.IsNullOrWhiteSpace(x)));
        });

        // p3: 1..15 => "30", 16..31 => "НБ"
        var r3 = rows.Single(r => r.RNOKPP == "333");
        Assert.All(r3.MainCodes.Take(15), c => Assert.Equal("30", c));
        Assert.All(r3.MainCodes.Skip(15), c => Assert.Equal("НБ", c));

        // p2: 1..9 => "НБ", 10..31 => "30"
        var r2 = rows.Single(r => r.RNOKPP == "222");
        Assert.All(r2.MainCodes.Take(9), c => Assert.Equal("НБ", c));
        Assert.All(r2.MainCodes.Skip(9), c => Assert.Equal("30", c));
    }

    [Fact]
    public async Task GetTimesheetMonthAsync_applies_search_by_rnokpp_or_fullname()
    {
        await using var tdb = new SqliteTestDb();

        using (var db = tdb.Factory.CreateDbContext())
        {
            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));
            var p2 = NewPerson("222", "Petrenko Petro", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));

            db.PersonRead.AddRange(p1, p2);

            var t1m = NewTimeline(p1.Id, TimesheetLane.Main, openedAt: new DateOnly(2026, 01, 01));
            var t2m = NewTimeline(p2.Id, TimesheetLane.Main, openedAt: new DateOnly(2026, 01, 01));

            db.TimesheetTimelines.AddRange(t1m, t2m);

            db.TimesheetEntries.AddRange(
                NewEntry(t1m, p1.Id, TimesheetLane.Main, "30", new DateOnly(2026, 01, 01), null),
                NewEntry(t2m, p2.Id, TimesheetLane.Main, "30", new DateOnly(2026, 01, 01), null)
            );

            db.SaveChanges();
        }

        var repo = new TimesheetMonthRepository(tdb.Factory);
        var daysInMonth = DateTime.DaysInMonth(2026, 1);

        var byRnokpp = await repo.GetTimesheetMonthAsync(2026, 1, "111");
        Assert.Single(byRnokpp);
        Assert.Equal("111", byRnokpp[0].RNOKPP);
        Assert.Equal(daysInMonth, byRnokpp[0].MainRef.Count);
        Assert.All(byRnokpp[0].MainRef, x => Assert.True(string.IsNullOrWhiteSpace(x)));

        var byName = await repo.GetTimesheetMonthAsync(2026, 1, "petrenko");
        Assert.Single(byName);
        Assert.Equal("222", byName[0].RNOKPP);
        Assert.Equal(daysInMonth, byName[0].MainRef.Count);
        Assert.All(byName[0].MainRef, x => Assert.True(string.IsNullOrWhiteSpace(x)));
    }

    [Fact]
    public async Task GetTimesheetMonthAsync_sets_MainRef100_only_for_alert_codes()
    {
        await using var tdb = new SqliteTestDb();

        using (var db = tdb.Factory.CreateDbContext())
        {
            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));
            db.PersonRead.Add(p1);

            var t1m = NewTimeline(p1.Id, TimesheetLane.Main, openedAt: new DateOnly(2026, 01, 01));
            db.TimesheetTimelines.Add(t1m);

            db.TimesheetEntries.AddRange(
                NewEntry(t1m, p1.Id, TimesheetLane.Main, "30", from: new DateOnly(2026, 01, 01), to: new DateOnly(2026, 01, 04)),
                NewEntry(t1m, p1.Id, TimesheetLane.Main, "100", from: new DateOnly(2026, 01, 05), to: new DateOnly(2026, 01, 05), reference: "REF-ABC"),
                NewEntry(t1m, p1.Id, TimesheetLane.Main, "30", from: new DateOnly(2026, 01, 06), to: null)
            );

            db.SaveChanges();
        }

        var repo = new TimesheetMonthRepository(tdb.Factory);

        var rows = await repo.GetTimesheetMonthAsync(2026, 1, search: "111");
        Assert.Single(rows);

        var r = rows[0];

        // day 5 => index 4
        Assert.Equal("100", r.MainCodes[4]);
        Assert.Equal("REF-ABC", r.MainRef[4]);

        // neighbors are not alert => ref must be empty/null
        Assert.True(string.IsNullOrWhiteSpace(r.MainRef[3]));
        Assert.True(string.IsNullOrWhiteSpace(r.MainRef[5]));
    }

    // -------------------------
    // Test data helpers
    // -------------------------

    private static PersonReadModel NewPerson(
        string rnokpp,
        string fullName,
        EnrollmentKind kind,
        DateOnly enrolledAt,
        DateOnly? excludedAt = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Rnokpp = rnokpp,
            FullName = fullName,
            LastName = fullName.Split(' ')[0],
            FirstName = fullName.Split(' ').Length > 1 ? fullName.Split(' ')[1] : "X",
            Lifecycle = excludedAt is null ? PersonLifecycle.Enrolled : PersonLifecycle.Reserved,
            EnrollmentKind = kind,
            EnrolledAt = enrolledAt,
            ExcludedAt = excludedAt,
            PositionSort = 0,
            Version = 1,
            UpdatedAtUtc = NowUtc
        };

    private static TimesheetTimeline NewTimeline(
        Guid personId,
        TimesheetLane lane,
        DateOnly openedAt,
        DateOnly? closedAt = null)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Lane = lane,
            OpenedAt = openedAt,
            ClosedAt = closedAt
        };

    private static TimesheetEntry NewEntry(
        TimesheetTimeline timeline,
        Guid personId,
        TimesheetLane lane,
        string code,
        DateOnly from,
        DateOnly? to,
        string? reference = null)
        => new()
        {
            Id = Guid.NewGuid(),
            TimelineId = timeline.Id,
            PersonId = personId,
            Lane = lane,
            Code = code,
            From = from,
            To = to,
            Reference = reference,
            CreatedBy = "tester",
            CreatedAtUtc = NowUtc
        };
}
