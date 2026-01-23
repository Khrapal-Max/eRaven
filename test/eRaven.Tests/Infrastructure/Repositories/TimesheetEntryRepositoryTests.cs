//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntryRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetEntryRepositoryTests
{
    private static TimesheetTimeline NewTimeline(Guid personId, TimesheetLane lane, DateOnly openedAt, DateOnly? closedAt = null)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Lane = lane,
            OpenedAt = openedAt,
            ClosedAt = closedAt
        };

    private static TimesheetEntry NewEntry(
        Guid timelineId,
        Guid personId,
        TimesheetLane lane,
        string code,
        DateOnly from,
        DateOnly? to = null,
        bool isDeleted = false)
        => new()
        {
            Id = Guid.NewGuid(),
            TimelineId = timelineId,
            PersonId = personId,
            Lane = lane,
            Code = code,
            From = from,
            To = to,
            Reference = null,
            Note = null,
            CreatedBy = "test",
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = isDeleted
        };

    [Fact]
    public async Task GetByIdAsync_ignores_soft_deleted()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var tl = NewTimeline(personId, TimesheetLane.Main, new DateOnly(2026, 1, 1));

        TimesheetEntry deleted;

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetTimelines.Add(tl);

            deleted = NewEntry(tl.Id, personId, TimesheetLane.Main, "30", new DateOnly(2026, 1, 1), null, isDeleted: true);
            db.TimesheetEntries.Add(deleted);

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var got = await repo.GetByIdAsync(deleted.Id);

        Assert.Null(got);
    }

    [Fact]
    public async Task GetPersonEntriesAsync_returns_overlapping_entries_sorted()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var mainTl = NewTimeline(personId, TimesheetLane.Main, new DateOnly(2026, 1, 1));
        var taskTl = NewTimeline(personId, TimesheetLane.Task, new DateOnly(2026, 1, 1));

        TimesheetEntry eMain2;
        TimesheetEntry eMain1;
        TimesheetEntry eTask;

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetTimelines.AddRange(mainTl, taskTl);

            // main entries (out of order in insert)
            eMain2 = NewEntry(mainTl.Id, personId, TimesheetLane.Main, "ВП", new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 12));
            eMain1 = NewEntry(mainTl.Id, personId, TimesheetLane.Main, "30", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 9));

            // task entry overlaps too
            eTask = NewEntry(taskTl.Id, personId, TimesheetLane.Task, "ПБД", new DateOnly(2026, 1, 5), null);

            db.TimesheetEntries.AddRange(eMain2, eTask, eMain1);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var res = await repo.GetPersonEntriesAsync(
            personId,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 1, 31));

        // sorted by Lane then From then Id
        Assert.Equal(3, res.Count);
        Assert.Equal(TimesheetLane.Main, res[0].Lane);
        Assert.Equal("30", res[0].Code);
        Assert.Equal(new DateOnly(2026, 1, 1), res[0].From);

        Assert.Equal(TimesheetLane.Main, res[1].Lane);
        Assert.Equal("ВП", res[1].Code);
        Assert.Equal(new DateOnly(2026, 1, 10), res[1].From);

        Assert.Equal(TimesheetLane.Task, res[2].Lane);
        Assert.Equal("ПБД", res[2].Code);
    }

    [Fact]
    public async Task GetActiveEntryOnDateAsync_returns_latest_by_From_when_multiple_overlap()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var tl = NewTimeline(personId, TimesheetLane.Main, new DateOnly(2026, 1, 1));

        TimesheetEntry old;
        TimesheetEntry newer;

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetTimelines.Add(tl);

            old = NewEntry(tl.Id, personId, TimesheetLane.Main, "30", new DateOnly(2026, 1, 1), null);
            newer = NewEntry(tl.Id, personId, TimesheetLane.Main, "ВП", new DateOnly(2026, 1, 10), null);

            db.TimesheetEntries.AddRange(old, newer);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var active = await repo.GetActiveEntryOnDateAsync(personId, TimesheetLane.Main, new DateOnly(2026, 1, 15));

        Assert.NotNull(active);
        Assert.Equal("ВП", active!.Code);
        Assert.Equal(new DateOnly(2026, 1, 10), active.From);
    }

    [Fact]
    public async Task SoftDeleteAsync_marks_deleted_and_excludes_from_queries()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var tl = NewTimeline(personId, TimesheetLane.Main, new DateOnly(2026, 1, 1));
        TimesheetEntry e;

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetTimelines.Add(tl);
            e = NewEntry(tl.Id, personId, TimesheetLane.Main, "30", new DateOnly(2026, 1, 1), null);
            db.TimesheetEntries.Add(e);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        await repo.SoftDeleteAsync(
            entryId: e.Id,
            reason: "test delete",
            author: "ui",
            nowUtc: DateTime.UtcNow);

        // GetByIdAsync should ignore deleted
        var got = await repo.GetByIdAsync(e.Id);
        Assert.Null(got);

        // List should not contain deleted
        var list = await repo.GetPersonEntriesAsync(personId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));
        Assert.Empty(list);

        // DB marked deleted
        using var db2 = tdb.Factory.CreateDbContext();
        var stored = await db2.TimesheetEntries.FindAsync(e.Id);

        Assert.NotNull(stored);
        Assert.True(stored!.IsDeleted);
        Assert.Equal("ui", stored.DeletedBy);
        Assert.Equal("test delete", stored.DeleteReason);
        Assert.NotNull(stored.DeletedAtUtc);
    }

    [Fact]
    public async Task GetEntriesForPersonsAsync_returns_entries_for_all_persons_in_period()
    {
        await using var tdb = new SqliteTestDb();

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var tl1 = NewTimeline(p1, TimesheetLane.Main, new DateOnly(2026, 1, 1));
        var tl2 = NewTimeline(p2, TimesheetLane.Main, new DateOnly(2026, 1, 1));

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetTimelines.AddRange(tl1, tl2);

            db.TimesheetEntries.AddRange(
                NewEntry(tl1.Id, p1, TimesheetLane.Main, "30", new DateOnly(2026, 1, 1), null),
                NewEntry(tl2.Id, p2, TimesheetLane.Main, "РОЗПОР", new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 20))
            );

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var res = await repo.GetEntriesForPersonsAsync(
            personIds: [p1, p2],
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 1, 31));

        Assert.Equal(2, res.Count);
        Assert.Contains(res, x => x.PersonId == p1 && x.Code == "30");
        Assert.Contains(res, x => x.PersonId == p2 && x.Code == "РОЗПОР");
    }
}
