//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTimelineRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetTimelineRepositoryTests
{
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

    [Fact]
    public async Task GetActiveTimelineAsync_returns_only_active_closedAt_null()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetTimelines.AddRange(
                NewTimeline(personId, TimesheetLane.Main, new DateOnly(2026, 1, 1), closedAt: new DateOnly(2026, 1, 10)),
                NewTimeline(personId, TimesheetLane.Main, new DateOnly(2026, 1, 11), closedAt: null) // active
            );

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetTimelineRepository(tdb.Factory);

        var active = await repo.GetActiveTimelineAsync(personId, TimesheetLane.Main);

        Assert.NotNull(active);
        Assert.Equal(personId, active!.PersonId);
        Assert.Equal(TimesheetLane.Main, active.Lane);
        Assert.Null(active.ClosedAt);
        Assert.Equal(new DateOnly(2026, 1, 11), active.OpenedAt);
    }

    [Fact]
    public async Task GetTimelineOnDateAsync_returns_timeline_that_covers_date()
    {
        await using var tdb = new SqliteTestDb();
        var personId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetTimelines.AddRange(
                NewTimeline(personId, TimesheetLane.Main, new DateOnly(2026, 1, 1), closedAt: new DateOnly(2026, 1, 10)),
                NewTimeline(personId, TimesheetLane.Main, new DateOnly(2026, 1, 11), closedAt: null)
            );
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetTimelineRepository(tdb.Factory);

        var t1 = await repo.GetTimelineOnDateAsync(personId, TimesheetLane.Main, new DateOnly(2026, 1, 5));
        Assert.NotNull(t1);
        Assert.Equal(new DateOnly(2026, 1, 1), t1!.OpenedAt);
        Assert.Equal(new DateOnly(2026, 1, 10), t1.ClosedAt);

        var t2 = await repo.GetTimelineOnDateAsync(personId, TimesheetLane.Main, new DateOnly(2026, 1, 15));
        Assert.NotNull(t2);
        Assert.Equal(new DateOnly(2026, 1, 11), t2!.OpenedAt);
        Assert.Null(t2.ClosedAt);
    }

    [Fact]
    public async Task GetPersonIdsOverlappingAsync_returns_distinct_persons()
    {
        await using var tdb = new SqliteTestDb();

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetTimelines.AddRange(
                // overlaps Jan
                NewTimeline(p1, TimesheetLane.Main, new DateOnly(2026, 1, 1), closedAt: null),
                // overlaps Jan partially
                NewTimeline(p2, TimesheetLane.Main, new DateOnly(2025, 12, 20), closedAt: new DateOnly(2026, 1, 5)),
                // different lane => should not count for Main query
                NewTimeline(p1, TimesheetLane.Task, new DateOnly(2026, 1, 1), closedAt: null)
            );

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetTimelineRepository(tdb.Factory);

        var ids = await repo.GetPersonIdsOverlappingAsync(
            lane: TimesheetLane.Main,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 1, 31));

        Assert.Equal(2, ids.Count);
        Assert.Contains(p1, ids);
        Assert.Contains(p2, ids);
    }
}
