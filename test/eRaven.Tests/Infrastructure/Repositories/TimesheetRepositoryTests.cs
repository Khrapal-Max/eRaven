//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetRepositoryTests
{
    private static TimesheetEntry NewEntry(
        Guid personId,
        TimesheetLane lane,
        string code,
        DateOnly from,
        DateOnly? to,
        bool isDeleted = false)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Lane = lane,
            Code = code,
            From = from,
            To = to,
            Reference = null,
            Note = null,
            CreatedBy = "seed",
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = isDeleted
        };

    [Fact]
    public async Task CreateEntryAsync_throws_on_overlap_with_open_ended_entry_same_lane()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var existing = NewEntry(personId, TimesheetLane.Main, "ВП", new DateOnly(2026, 1, 1), to: null);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetEntries.Add(existing);
            await db.SaveChangesAsync();
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await repo.CreateEntryAsync(
                personId: personId,
                lane: TimesheetLane.Main,
                code: "30",
                from: new DateOnly(2026, 1, 10),
                to: null,
                reference: null,
                note: null,
                author: "ui",
                nowUtc: DateTime.UtcNow));

        Assert.Contains("перетинається", ex.Message);
    }

    [Fact]
    public async Task CreateEntryAsync_allows_same_dates_in_other_lane()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var existingMain = NewEntry(personId, TimesheetLane.Main, "30", new DateOnly(2026, 1, 1), to: null);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetEntries.Add(existingMain);
            await db.SaveChangesAsync();
        }

        var id = await repo.CreateEntryAsync(
            personId: personId,
            lane: TimesheetLane.Task,
            code: "PLAN",
            from: new DateOnly(2026, 1, 10),
            to: null,
            reference: "x",
            note: null,
            author: "ui",
            nowUtc: DateTime.UtcNow);

        using var db2 = tdb.Factory.CreateDbContext();
        var created = await db2.TimesheetEntries.SingleAsync(x => x.Id == id);

        Assert.Equal(TimesheetLane.Task, created.Lane);
        Assert.Equal("PLAN", created.Code);
    }

    [Fact]
    public async Task UpdateEntryAsync_throws_on_overlap_after_change()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var e1 = NewEntry(personId, TimesheetLane.Main, "ВП", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 9));
        var e2 = NewEntry(personId, TimesheetLane.Main, "30", new DateOnly(2026, 1, 10), null);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetEntries.AddRange(e1, e2);
            await db.SaveChangesAsync();
        }

        // пробуємо змістити e2 назад так, щоб перетинав e1
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await repo.UpdateEntryAsync(
                entryId: e2.Id,
                lane: TimesheetLane.Main,
                code: "30",
                from: new DateOnly(2026, 1, 5),
                to: null,
                reference: null,
                note: null,
                author: "ui",
                nowUtc: DateTime.UtcNow));

        Assert.Contains("перетинається", ex.Message);
    }

    [Fact]
    public async Task DeleteEntryAsync_soft_deletes_entry()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var e = NewEntry(personId, TimesheetLane.Main, "30", new DateOnly(2026, 1, 1), null);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetEntries.Add(e);
            await db.SaveChangesAsync();
        }

        var now = DateTime.UtcNow;

        await repo.DeleteEntryAsync(
            entryId: e.Id,
            reason: "test",
            author: "ui",
            nowUtc: now);

        using var db2 = tdb.Factory.CreateDbContext();
        var deleted = await db2.TimesheetEntries.SingleAsync(x => x.Id == e.Id);

        Assert.True(deleted.IsDeleted);
        Assert.Equal("ui", deleted.DeletedBy);
        Assert.NotNull(deleted.DeletedAtUtc);
        Assert.Equal("test", deleted.DeleteReason);
    }

    [Fact]
    public async Task EnsureOpenedOnEnrollAsync_creates_default_30_from_enrollDate_when_no_overlap()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 1, 10);
        var now = DateTime.UtcNow;

        await repo.EnsureOpenedOnEnrollAsync(personId, enrollDate, "ui", now);

        using var db = tdb.Factory.CreateDbContext();
        var main = await db.TimesheetEntries
            .Where(x => x.PersonId == personId && x.Lane == TimesheetLane.Main && !x.IsDeleted)
            .OrderBy(x => x.From)
            .ToListAsync();

        Assert.Single(main);
        Assert.Equal("30", main[0].Code);
        Assert.Equal(enrollDate, main[0].From);
        Assert.Null(main[0].To);
    }

    [Fact]
    public async Task EnsureOpenedOnEnrollAsync_clamps_previous_open_entry_and_creates_30()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 1, 10);
        var now = DateTime.UtcNow;

        var old = NewEntry(personId, TimesheetLane.Main, "ВП", new DateOnly(2026, 1, 1), to: null);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetEntries.Add(old);
            await db.SaveChangesAsync();
        }

        await repo.EnsureOpenedOnEnrollAsync(personId, enrollDate, "ui", now);

        using var db2 = tdb.Factory.CreateDbContext();

        var entries = await db2.TimesheetEntries
            .Where(x => x.PersonId == personId && x.Lane == TimesheetLane.Main && !x.IsDeleted)
            .OrderBy(x => x.From)
            .ToListAsync();

        Assert.Equal(2, entries.Count);

        Assert.Equal("ВП", entries[0].Code);
        Assert.Equal(new DateOnly(2026, 1, 1), entries[0].From);
        Assert.Equal(enrollDate.AddDays(-1), entries[0].To); // clamp

        Assert.Equal("30", entries[1].Code);
        Assert.Equal(enrollDate, entries[1].From);
        Assert.Null(entries[1].To);
    }

    [Fact]
    public async Task EnsureClosedOnExcludeAsync_clamps_open_entries_and_soft_deletes_future_entries()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var closeTo = new DateOnly(2026, 1, 10);
        var now = DateTime.UtcNow;

        var open = NewEntry(personId, TimesheetLane.Main, "30", new DateOnly(2026, 1, 1), to: null);
        var future = NewEntry(personId, TimesheetLane.Main, "ВП", new DateOnly(2026, 1, 20), to: null);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetEntries.AddRange(open, future);
            await db.SaveChangesAsync();
        }

        await repo.EnsureClosedOnExcludeAsync(personId, closeTo, "excluded", "ui", now);

        using var db2 = tdb.Factory.CreateDbContext();

        var openAfter = await db2.TimesheetEntries.SingleAsync(x => x.Id == open.Id);
        Assert.Equal(closeTo, openAfter.To);
        Assert.Equal("ui", openAfter.UpdatedBy);
        Assert.NotNull(openAfter.UpdatedAtUtc);

        var futureAfter = await db2.TimesheetEntries.SingleAsync(x => x.Id == future.Id);
        Assert.True(futureAfter.IsDeleted);
        Assert.Equal("ui", futureAfter.DeletedBy);
        Assert.NotNull(futureAfter.DeletedAtUtc);
        Assert.Contains("excluded", futureAfter.DeleteReason ?? "");
    }

    [Fact]
    public async Task GetPersonEntriesAsync_returns_entries_overlapping_range_ordered()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetRepository(tdb.Factory);

        var personId = Guid.NewGuid();

        var e1 = NewEntry(personId, TimesheetLane.Main, "ВП", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 9));
        var e2 = NewEntry(personId, TimesheetLane.Main, "30", new DateOnly(2026, 1, 10), null);
        var e3 = NewEntry(personId, TimesheetLane.Task, "PLAN", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6));

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetEntries.AddRange(e1, e2, e3);
            await db.SaveChangesAsync();
        }

        var list = await repo.GetPersonEntriesAsync(personId, new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 10));

        // overlap should include e1 (covers 6), e3 (covers 6), e2 (starts 10)
        Assert.Equal(3, list.Count);
        Assert.Equal(TimesheetLane.Main, list[0].Lane);
        Assert.Equal(TimesheetLane.Main, list[1].Lane);
        Assert.Equal(TimesheetLane.Task, list[2].Lane); // because ordering by Lane then From

        Assert.Contains(list, x => x.Id == e1.Id);
        Assert.Contains(list, x => x.Id == e2.Id);
        Assert.Contains(list, x => x.Id == e3.Id);
    }

    [Fact]
    public async Task CreateEntryAsync_disallows_setting_NB_code()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetRepository(tdb.Factory);

        var personId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await repo.CreateEntryAsync(
                personId: personId,
                lane: TimesheetLane.Main,
                code: "НБ",
                from: new DateOnly(2026, 1, 1),
                to: null,
                reference: null,
                note: null,
                author: "ui",
                nowUtc: DateTime.UtcNow));

        Assert.Contains("НБ", ex.Message);
        Assert.Contains("системним", ex.Message);
    }

    [Fact]
    public async Task UpdateEntryAsync_disallows_setting_NB_code()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var entry = NewEntry(personId, TimesheetLane.Main, "30", new DateOnly(2026, 1, 1), null);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetEntries.Add(entry);
            await db.SaveChangesAsync();
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await repo.UpdateEntryAsync(
                entryId: entry.Id,
                lane: TimesheetLane.Main,
                code: "НБ",
                from: new DateOnly(2026, 1, 1),
                to: null,
                reference: null,
                note: null,
                author: "ui",
                nowUtc: DateTime.UtcNow));

        Assert.Contains("НБ", ex.Message);
    }

    [Fact]
    public async Task EnsureOpenedOnEnrollAsync_creates_Main_30_entry()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 1, 10);
        var now = DateTime.UtcNow;

        await repo.EnsureOpenedOnEnrollAsync(personId, enrollDate, "ui", now);

        using var db = tdb.Factory.CreateDbContext();
        var e = await db.TimesheetEntries
            .Where(x => x.PersonId == personId && x.Lane == TimesheetLane.Main && !x.IsDeleted)
            .SingleAsync();

        Assert.Equal("30", e.Code);
        Assert.Equal(enrollDate, e.From);
        Assert.Null(e.To);
    }

    [Fact]
    public async Task EnsureClosedOnExcludeAsync_results_in_no_future_main_entries_after_close_date()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var closeTo = new DateOnly(2026, 1, 10);
        var now = DateTime.UtcNow;

        var open = NewEntry(personId, TimesheetLane.Main, "30", new DateOnly(2026, 1, 1), null);
        var future = NewEntry(personId, TimesheetLane.Main, "ВП", new DateOnly(2026, 1, 20), null);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetEntries.AddRange(open, future);
            await db.SaveChangesAsync();
        }

        await repo.EnsureClosedOnExcludeAsync(personId, closeTo, "excluded", "ui", now);

        using var db2 = tdb.Factory.CreateDbContext();

        var openAfter = await db2.TimesheetEntries.SingleAsync(x => x.Id == open.Id);
        Assert.Equal(closeTo, openAfter.To);

        var futureAfter = await db2.TimesheetEntries.SingleAsync(x => x.Id == future.Id);
        Assert.True(futureAfter.IsDeleted);

        // ключова перевірка: після closeTo НЕ має бути активних Main entry
        var anyActiveAfter = await db2.TimesheetEntries
            .AsNoTracking()
            .AnyAsync(x => x.PersonId == personId
                           && x.Lane == TimesheetLane.Main
                           && !x.IsDeleted
                           && x.From > closeTo);

        Assert.False(anyActiveAfter);
    }

    [Fact]
    public async Task EnsureClosedOnExcludeAsync_disallows_exclude_when_main_code_not_allowed()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetRepository(tdb.Factory);

        var personId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetEntries.Add(new TimesheetEntry
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                Lane = TimesheetLane.Main,
                Code = "ВП",
                From = new DateOnly(2026, 1, 1),
                To = null,
                CreatedBy = "t",
                CreatedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.EnsureClosedOnExcludeAsync(
                personId: personId,
                closeTo: new DateOnly(2026, 1, 10),
                reason: "test",
                author: "ui",
                nowUtc: DateTime.UtcNow));

        Assert.Contains("30", ex.Message);
        Assert.Contains("РОЗПОР", ex.Message);
        Assert.Contains("ВП", ex.Message);
    }

    [Fact]
    public async Task EnsureClosedOnExcludeAsync_allows_exclude_when_main_code_30()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var closeTo = new DateOnly(2026, 1, 10);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetEntries.Add(new TimesheetEntry
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                Lane = TimesheetLane.Main,
                Code = "30",
                From = new DateOnly(2026, 1, 1),
                To = null,
                CreatedBy = "t",
                CreatedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        await repo.EnsureClosedOnExcludeAsync(personId, closeTo, "test", "ui", DateTime.UtcNow);

        using (var db = tdb.Factory.CreateDbContext())
        {
            var main = db.TimesheetEntries.Single(x => x.PersonId == personId && x.Lane == TimesheetLane.Main);
            Assert.Equal(closeTo, main.To);
        }
    }
}
