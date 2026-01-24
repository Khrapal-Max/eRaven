//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetMonthGridRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetMonthGridRepositoryTests
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
            var p3 = NewPerson("333", "Sydorenko Sydir", EnrollmentKind.Unit, new DateOnly(2026, 01, 01), excludedAt: new DateOnly(2026, 01, 15));

            db.PersonRead.AddRange(p1, p2, p3);

            var t1m = NewTimeline(p1.Id, TimesheetLane.Main, openedAt: new DateOnly(2026, 01, 01));
            var t1t = NewTimeline(p1.Id, TimesheetLane.Task, openedAt: new DateOnly(2026, 01, 01));

            var t2m = NewTimeline(p2.Id, TimesheetLane.Main, openedAt: new DateOnly(2026, 01, 10));
            var t2t = NewTimeline(p2.Id, TimesheetLane.Task, openedAt: new DateOnly(2026, 01, 10));

            var t3m = NewTimeline(p3.Id, TimesheetLane.Main, openedAt: new DateOnly(2026, 01, 01), closedAt: new DateOnly(2026, 01, 15));
            var t3t = NewTimeline(p3.Id, TimesheetLane.Task, openedAt: new DateOnly(2026, 01, 01), closedAt: new DateOnly(2026, 01, 15));

            db.TimesheetTimelines.AddRange(t1m, t1t, t2m, t2t, t3m, t3t);

            db.TimesheetEntries.AddRange(
                NewEntry(t1m, p1.Id, TimesheetLane.Main, "30", from: new DateOnly(2026, 01, 01), to: null),
                NewEntry(t2m, p2.Id, TimesheetLane.Main, "30", from: new DateOnly(2026, 01, 10), to: null),
                NewEntry(t3m, p3.Id, TimesheetLane.Main, "30", from: new DateOnly(2026, 01, 01), to: new DateOnly(2026, 01, 15))
            );

            db.SaveChanges();
        }

        var repo = new TimesheetMonthGridRepository(tdb.Factory);

        var res = await repo.GetTimesheetMonthAsync(2026, 1, search: null);

        Assert.Equal(31, res.DaysInMonth);
        Assert.Equal(3, res.Rows.Count);

        var r3 = res.Rows.Single(r => r.RNOKPP == "333");
        Assert.All(r3.MainCodes.Take(15), c => Assert.Equal("30", c));
        Assert.All(r3.MainCodes.Skip(15), c => Assert.Equal("НБ", c));

        var r2 = res.Rows.Single(r => r.RNOKPP == "222");
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

        var repo = new TimesheetMonthGridRepository(tdb.Factory);

        var byRnokpp = await repo.GetTimesheetMonthAsync(2026, 1, "111");
        Assert.Single(byRnokpp.Rows);
        Assert.Equal("111", byRnokpp.Rows[0].RNOKPP);

        var byName = await repo.GetTimesheetMonthAsync(2026, 1, "petrenko");
        Assert.Single(byName.Rows);
        Assert.Equal("222", byName.Rows[0].RNOKPP);
    }

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
        DateOnly? to)
        => new()
        {
            Id = Guid.NewGuid(),
            TimelineId = timeline.Id,
            PersonId = personId,
            Lane = lane,
            Code = code,
            From = from,
            To = to,
            CreatedBy = "tester",
            CreatedAtUtc = NowUtc
        };
}
