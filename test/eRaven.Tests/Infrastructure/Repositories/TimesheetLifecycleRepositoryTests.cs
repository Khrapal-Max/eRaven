//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//----------------------------------------------------------------------------- 
// TimesheetLifecycleRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetLifecycleRepositoryTests
{
    [Fact]
    public async Task OpenOnEnrollAsync_creates_timeline_and_default_T()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetLifecycleRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 1, 10);
        var nowUtc = new DateTime(2026, 1, 10, 10, 0, 0, DateTimeKind.Utc);

        await repo.OpenOnEnrollAsync(personId, enrollDate, author: "ui", nowUtc: nowUtc);

        await using var db = await tdb.Factory.CreateDbContextAsync();

        var timelines = await db.TimesheetTimelines
            .AsNoTracking()
            .Where(x => x.PersonId == personId)
            .ToListAsync();

        Assert.Single(timelines);

        var tl = timelines[0];
        Assert.Equal(enrollDate, tl.OpenedAt);
        Assert.Null(tl.ClosedAt);

        var entries = await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => x.PersonId == personId && !x.IsDeleted)
            .ToListAsync();

        Assert.Single(entries);

        var e = entries[0];
        Assert.Equal(tl.Id, e.TimelineId);
        Assert.Equal("Т", e.Code);
        Assert.Equal(enrollDate, e.From);
        Assert.Null(e.To);
        Assert.Equal("Auto: system", e.Reference);
    }

    [Fact]
    public async Task OpenOnEnrollAsync_is_idempotent()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetLifecycleRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 1, 10);

        await repo.OpenOnEnrollAsync(personId, enrollDate, author: "ui", nowUtc: DateTime.UtcNow);
        await repo.OpenOnEnrollAsync(personId, enrollDate, author: "ui", nowUtc: DateTime.UtcNow);

        await using var db = await tdb.Factory.CreateDbContextAsync();
        Assert.Equal(1, await db.TimesheetTimelines.CountAsync(x => x.PersonId == personId));
        Assert.Equal(1, await db.TimesheetEntries.CountAsync(x => x.PersonId == personId && !x.IsDeleted));
    }

    [Fact]
    public async Task ValidateCanCloseOnExcludeAsync_throws_when_code_not_allowed()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetLifecycleRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var openedAt = new DateOnly(2026, 1, 1);

        await using (var db = await tdb.Factory.CreateDbContextAsync())
        {
            var tl = NewTimeline(personId, openedAt, nowUtc);
            db.TimesheetTimelines.Add(tl);

            db.TimesheetEntries.Add(NewEntry(
                timelineId: tl.Id,
                personId: personId,
                code: "ВП",
                from: openedAt,
                to: null,
                nowUtc: nowUtc));

            await db.SaveChangesAsync();
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await repo.ValidateCanCloseOnExcludeAsync(personId, closeTo: new DateOnly(2026, 1, 5)));

        Assert.Contains("ВП", ex.Message);
        Assert.Contains("30", ex.Message);
        Assert.Contains("РОЗПОР", ex.Message);
    }

    [Fact]
    public async Task CloseOnExcludeAsync_closes_timeline_clamps_and_deletes_future_entries()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetLifecycleRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var openedAt = new DateOnly(2026, 1, 1);
        var closeTo = new DateOnly(2026, 1, 15);

        Guid tlId;

        await using (var db = await tdb.Factory.CreateDbContextAsync())
        {
            var tl = NewTimeline(personId, openedAt, nowUtc);
            tlId = tl.Id;

            db.TimesheetTimelines.Add(tl);

            // 30 [1..4], РОЗПОР [5..∞] (allowed to close)
            db.TimesheetEntries.Add(NewEntry(tl.Id, personId, "30", openedAt, new DateOnly(2026, 1, 4), nowUtc));
            db.TimesheetEntries.Add(NewEntry(tl.Id, personId, "РОЗПОР", new DateOnly(2026, 1, 5), null, nowUtc));

            // future plan must be deleted
            db.TimesheetEntries.Add(NewEntry(tl.Id, personId, "ПЛАН", new DateOnly(2026, 1, 16), new DateOnly(2026, 1, 20), nowUtc));

            await db.SaveChangesAsync();
        }

        await repo.CloseOnExcludeAsync(personId, closeTo, reason: "test", author: "ui", nowUtc: DateTime.UtcNow);

        await using (var db = await tdb.Factory.CreateDbContextAsync())
        {
            var tl = await db.TimesheetTimelines.AsNoTracking().SingleAsync(x => x.Id == tlId);
            Assert.Equal(closeTo, tl.ClosedAt);

            var rozpor = await db.TimesheetEntries.AsNoTracking()
                .SingleAsync(x => x.TimelineId == tlId && x.Code == "РОЗПОР");

            Assert.Equal(closeTo, rozpor.To);

            var futurePlan = await db.TimesheetEntries.AsNoTracking()
                .SingleAsync(x => x.TimelineId == tlId && x.Code == "ПЛАН");

            Assert.True(futurePlan.IsDeleted);
            Assert.Contains("excluded", futurePlan.DeleteReason ?? "");
        }
    }

    private static TimesheetTimeline NewTimeline(Guid personId, DateOnly openedAt, DateTime nowUtc)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            OpenedAt = openedAt,
            ClosedAt = null,
            CreatedBy = "seed",
            CreatedAtUtc = nowUtc
        };

    private static TimesheetEntry NewEntry(
        Guid timelineId,
        Guid personId,
        string code,
        DateOnly from,
        DateOnly? to,
        DateTime nowUtc)
        => new()
        {
            Id = Guid.NewGuid(),
            TimelineId = timelineId,
            PersonId = personId,
            Code = code,
            From = from,
            To = to,
            Reference = null,
            Note = null,
            CreatedBy = "seed",
            CreatedAtUtc = nowUtc,
            IsDeleted = false
        };
}
