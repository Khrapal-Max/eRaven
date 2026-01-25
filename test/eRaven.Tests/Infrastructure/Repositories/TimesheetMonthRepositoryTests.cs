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

    [Fact]
    public async Task GetTimesheetDayAsync_returns_only_persons_whose_MAIN_timeline_overlaps_date_and_applies_defaults()
    {
        await using var tdb = new SqliteTestDb();

        var date = new DateOnly(2026, 01, 12);

        Guid p1Id, p2Id, p3Id;

        using (var db = tdb.Factory.CreateDbContext())
        {
            // persons
            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));
            var p2 = NewPerson("222", "Petrenko Petro", EnrollmentKind.Unit, new DateOnly(2026, 01, 10));
            var p3 = NewPerson("333", "Sydorenko Sydir", EnrollmentKind.Unit, new DateOnly(2026, 01, 01), excludedAt: new DateOnly(2026, 01, 11));

            p1Id = p1.Id; p2Id = p2.Id; p3Id = p3.Id;

            db.PersonRead.AddRange(p1, p2, p3);

            // timelines (p3 closed before date => should be excluded)
            var p1m = NewTimeline(p1.Id, TimesheetLane.Main, openedAt: new DateOnly(2026, 01, 01));
            var p1t = NewTimeline(p1.Id, TimesheetLane.Task, openedAt: new DateOnly(2026, 01, 01));

            var p2m = NewTimeline(p2.Id, TimesheetLane.Main, openedAt: new DateOnly(2026, 01, 10));
            var p2t = NewTimeline(p2.Id, TimesheetLane.Task, openedAt: new DateOnly(2026, 01, 10));

            var p3m = NewTimeline(p3.Id, TimesheetLane.Main, openedAt: new DateOnly(2026, 01, 01), closedAt: new DateOnly(2026, 01, 11));
            var p3t = NewTimeline(p3.Id, TimesheetLane.Task, openedAt: new DateOnly(2026, 01, 01), closedAt: new DateOnly(2026, 01, 11));

            db.TimesheetTimelines.AddRange(p1m, p1t, p2m, p2t, p3m, p3t);

            // entries: only p1 has entries, p2 has none => defaults should kick in
            db.TimesheetEntries.AddRange(
                NewEntry(p1m, p1.Id, TimesheetLane.Main, "30", from: new DateOnly(2026, 01, 01), to: null, reference: " R-MAIN ", note: " N-MAIN "),
                NewEntry(p1t, p1.Id, TimesheetLane.Task, "T1", from: new DateOnly(2026, 01, 05), to: null, reference: " R-TASK ", note: " N-TASK ")
            );

            db.SaveChanges();
        }

        var repo = new TimesheetMonthRepository(tdb.Factory);

        // act
        var rows = await repo.GetTimesheetDayAsync(date, search: null);

        // assert: p1 + p2 only (p3 timeline already closed)
        Assert.Equal(2, rows.Count);

        var r1 = rows.Single(r => r.RNOKPP == "111");
        Assert.Equal(p1Id, r1.PersonId);
        Assert.Equal("30", r1.Main.Code);
        Assert.Equal("R-MAIN", r1.Main.Reference);
        Assert.Equal("N-MAIN", r1.Main.Note);

        Assert.Equal("T1", r1.Task.Code);
        Assert.Equal("R-TASK", r1.Task.Reference);
        Assert.Equal("N-TASK", r1.Task.Note);

        var r2 = rows.Single(r => r.RNOKPP == "222");
        Assert.Equal(p2Id, r2.PersonId);
        Assert.Equal("НБ", r2.Main.Code);
        Assert.Null(r2.Main.Reference);
        Assert.Null(r2.Main.Note);

        Assert.Equal("", r2.Task.Code);
        Assert.Null(r2.Task.Reference);
        Assert.Null(r2.Task.Note);

        Assert.DoesNotContain(rows, r => r.PersonId == p3Id);
    }

    [Fact]
    public async Task GetTimesheetDayAsync_picks_latest_active_entry_per_person_and_lane_on_date()
    {
        await using var tdb = new SqliteTestDb();

        var date = new DateOnly(2026, 01, 12);

        using (var db = tdb.Factory.CreateDbContext())
        {
            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));
            db.PersonRead.Add(p1);

            var tm = NewTimeline(p1.Id, TimesheetLane.Main, openedAt: new DateOnly(2026, 01, 01));
            var tt = NewTimeline(p1.Id, TimesheetLane.Task, openedAt: new DateOnly(2026, 01, 01));

            db.TimesheetTimelines.AddRange(tm, tt);

            // MAIN: older open-ended + newer open-ended (newer should win)
            db.TimesheetEntries.AddRange(
                NewEntry(tm, p1.Id, TimesheetLane.Main, "30", from: new DateOnly(2026, 01, 01), to: null, reference: "OLD", note: "OLDN"),
                NewEntry(tm, p1.Id, TimesheetLane.Main, "100", from: new DateOnly(2026, 01, 11), to: null, reference: " NEW-REF ", note: " NEW-NOTE ")
            );

            // TASK: older open-ended + newer starting on the date (newer should win)
            db.TimesheetEntries.AddRange(
                NewEntry(tt, p1.Id, TimesheetLane.Task, "A", from: new DateOnly(2026, 01, 05), to: null, reference: "TA-OLD", note: null),
                NewEntry(tt, p1.Id, TimesheetLane.Task, "B", from: new DateOnly(2026, 01, 12), to: null, reference: " TB-REF ", note: " TB-NOTE ")
            );

            db.SaveChanges();
        }

        var repo = new TimesheetMonthRepository(tdb.Factory);

        // act
        var rows = await repo.GetTimesheetDayAsync(date, search: null);

        // assert
        Assert.Single(rows);

        var r = rows[0];

        Assert.Equal("100", r.Main.Code);
        Assert.Equal("NEW-REF", r.Main.Reference);
        Assert.Equal("NEW-NOTE", r.Main.Note);

        Assert.Equal("B", r.Task.Code);
        Assert.Equal("TB-REF", r.Task.Reference);
        Assert.Equal("TB-NOTE", r.Task.Note);
    }

    [Fact]
    public async Task GetTimesheetDayAsync_applies_search_by_rnokpp_or_fullname()
    {
        await using var tdb = new SqliteTestDb();

        var date = new DateOnly(2026, 01, 12);

        using (var db = tdb.Factory.CreateDbContext())
        {
            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));
            var p2 = NewPerson("222", "Petrenko Petro", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));

            db.PersonRead.AddRange(p1, p2);

            var p1m = NewTimeline(p1.Id, TimesheetLane.Main, openedAt: new DateOnly(2026, 01, 01));
            var p2m = NewTimeline(p2.Id, TimesheetLane.Main, openedAt: new DateOnly(2026, 01, 01));

            db.TimesheetTimelines.AddRange(p1m, p2m);

            // at least one entry is not required, but ok to keep minimal
            db.TimesheetEntries.AddRange(
                NewEntry(p1m, p1.Id, TimesheetLane.Main, "30", new DateOnly(2026, 01, 01), null),
                NewEntry(p2m, p2.Id, TimesheetLane.Main, "30", new DateOnly(2026, 01, 01), null)
            );

            db.SaveChanges();
        }

        var repo = new TimesheetMonthRepository(tdb.Factory);

        var byRnokpp = await repo.GetTimesheetDayAsync(date, "111");
        Assert.Single(byRnokpp);
        Assert.Equal("111", byRnokpp[0].RNOKPP);

        var byName = await repo.GetTimesheetDayAsync(date, "petrenko");
        Assert.Single(byName);
        Assert.Equal("222", byName[0].RNOKPP);
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
        string? reference = null,
        string? note = null)
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
            Note = note,
            CreatedBy = "tester",
            CreatedAtUtc = NowUtc
        };
}
