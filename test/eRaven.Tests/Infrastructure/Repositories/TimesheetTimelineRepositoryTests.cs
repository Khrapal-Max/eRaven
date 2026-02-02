//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTimelineRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetTimelineRepositoryTests
{
    private static TimesheetTimeline NewTimeline(
        Guid personId,
        DateOnly openedAt,
        DateOnly? closedAt = null,
        string createdBy = "test",
        DateTime? createdAtUtc = null)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            OpenedAt = openedAt,
            ClosedAt = closedAt,

            CreatedBy = createdBy,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
            ClosedBy = closedAt is null ? null : createdBy,
            ClosedAtUtc = closedAt is null ? null : (createdAtUtc ?? DateTime.UtcNow)
        };

    [Fact]
    public async Task GetActiveTimelineAsync_returns_only_active_closedAt_null()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetTimelines.AddRange(
                NewTimeline(personId, new DateOnly(2026, 1, 1), closedAt: new DateOnly(2026, 1, 10)),
                NewTimeline(personId, new DateOnly(2026, 1, 11), closedAt: null) // active
            );

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetTimelineRepository(tdb.Factory);

        var active = await repo.GetActiveTimelineAsync(personId);

        Assert.NotNull(active);
        Assert.Equal(personId, active!.PersonId);
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
                NewTimeline(personId, new DateOnly(2026, 1, 1), closedAt: new DateOnly(2026, 1, 10)),
                NewTimeline(personId, new DateOnly(2026, 1, 11), closedAt: null)
            );

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetTimelineRepository(tdb.Factory);

        var t1 = await repo.GetTimelineOnDateAsync(personId, new DateOnly(2026, 1, 5));
        Assert.NotNull(t1);
        Assert.Equal(new DateOnly(2026, 1, 1), t1!.OpenedAt);
        Assert.Equal(new DateOnly(2026, 1, 10), t1.ClosedAt);

        var t2 = await repo.GetTimelineOnDateAsync(personId, new DateOnly(2026, 1, 15));
        Assert.NotNull(t2);
        Assert.Equal(new DateOnly(2026, 1, 11), t2!.OpenedAt);
        Assert.Null(t2.ClosedAt);
    }

    [Fact]
    public async Task GetTimelinesOverlappingAsync_returns_all_overlapping_timelines()
    {
        await using var tdb = new SqliteTestDb();

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetTimelines.AddRange(
                // overlaps Jan (open)
                NewTimeline(p1, new DateOnly(2026, 1, 1), closedAt: null),

                // overlaps Jan partially
                NewTimeline(p2, new DateOnly(2025, 12, 20), closedAt: new DateOnly(2026, 1, 5)),

                // does NOT overlap Jan (starts after)
                NewTimeline(p3, new DateOnly(2026, 2, 1), closedAt: null)
            );

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetTimelineRepository(tdb.Factory);

        var items = await repo.GetTimelinesOverlappingAsync(
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 1, 31));

        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.PersonId == p1);
        Assert.Contains(items, x => x.PersonId == p2);
        Assert.DoesNotContain(items, x => x.PersonId == p3);
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
                // p1: 2 різні (закриті) таймлайни, обидва перетинають січень,
                // але НЕ перетинаються між собою => валідна історія без 2-х активних.
                NewTimeline(p1, new DateOnly(2025, 12, 20), closedAt: new DateOnly(2026, 1, 5)),
                NewTimeline(p1, new DateOnly(2026, 1, 20), closedAt: new DateOnly(2026, 1, 25)),

                // p2: активний (open-ended) таймлайн у січні
                NewTimeline(p2, new DateOnly(2026, 1, 1), closedAt: null)
            );

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetTimelineRepository(tdb.Factory);

        var ids = await repo.GetPersonIdsOverlappingAsync(
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 1, 31));

        Assert.Equal(2, ids.Count);
        Assert.Contains(p1, ids);
        Assert.Contains(p2, ids);

        // додаткова перевірка, що реально distinct
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}
