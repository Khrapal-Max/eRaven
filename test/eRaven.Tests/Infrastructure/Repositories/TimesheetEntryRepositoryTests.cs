//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntryRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetEntryRepositoryTests
{
    private static readonly DateTime NowUtc = new(2026, 01, 23, 12, 0, 0, DateTimeKind.Utc);

    private static TimesheetTimeline NewTimeline(Guid personId, DateOnly openedAt, DateOnly? closedAt = null)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            OpenedAt = openedAt,
            ClosedAt = closedAt,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc
        };

    private static TimesheetCodeDefinition NewCode(
        string code,
        string? title = null,
        int sortOrder = 0,
        int priority = 0,
        bool isTerminal = false,
        bool isActive = true)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Title = title ?? code,
            Description = null,
            SortOrder = sortOrder,
            Priority = priority,
            IsTerminal = isTerminal,
            IsActive = isActive,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc
        };

    private static TimesheetEntry NewEntry(
        Guid timelineId,
        Guid personId,
        Guid codeId,
        DateOnly from,
        DateOnly? to = null,
        bool isDeleted = false)
        => new()
        {
            Id = Guid.NewGuid(),
            TimelineId = timelineId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeId,
            From = from,
            To = to,
            Reference = null,
            Note = null,
            CreatedBy = "test",
            CreatedAtUtc = NowUtc,
            IsDeleted = isDeleted
        };

    [Fact]
    public async Task GetByIdAsync_ignores_soft_deleted()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var tl = NewTimeline(personId, new DateOnly(2026, 1, 1));

        TimesheetEntry deleted;
        var c30 = NewCode("30");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(c30);
            db.TimesheetTimelines.Add(tl);

            deleted = NewEntry(tl.Id, personId, c30.Id, new DateOnly(2026, 1, 1), null, isDeleted: true);
            db.TimesheetEntries.Add(deleted);

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var got = await repo.GetByIdAsync(deleted.Id);

        Assert.Null(got);
    }

    [Fact]
    public async Task GetPersonEntriesAsync_returns_overlapping_entries_sorted_by_From_then_Id()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var tl = NewTimeline(personId, new DateOnly(2026, 1, 1));

        TimesheetEntry e3;
        TimesheetEntry e2;
        TimesheetEntry e1;
        TimesheetEntry outside; // should be filtered out

        var c30 = NewCode("30");
        var cPbd = NewCode("ПБД");
        var cVp = NewCode("ВП");
        var cX = NewCode("X");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(c30, cPbd, cVp, cX);
            db.TimesheetTimelines.Add(tl);

            // out of order insert:
            e3 = NewEntry(tl.Id, personId, cVp.Id, new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 12));
            e2 = NewEntry(tl.Id, personId, cPbd.Id, new DateOnly(2026, 1, 5), null);
            e1 = NewEntry(tl.Id, personId, c30.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 4));

            // outside of queried range (Feb)
            outside = NewEntry(tl.Id, personId, cX.Id, new DateOnly(2026, 2, 1), null);

            db.TimesheetEntries.AddRange(e3, e2, e1, outside);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var res = await repo.GetPersonEntriesAsync(
            personId,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 1, 31));

        Assert.Equal(3, res.Count);

        Assert.Equal(c30.Id, res[0].TimesheetCodeDefinitionId);
        Assert.Equal(new DateOnly(2026, 1, 1), res[0].From);

        Assert.Equal(cPbd.Id, res[1].TimesheetCodeDefinitionId);
        Assert.Equal(new DateOnly(2026, 1, 5), res[1].From);

        Assert.Equal(cVp.Id, res[2].TimesheetCodeDefinitionId);
        Assert.Equal(new DateOnly(2026, 1, 10), res[2].From);

        Assert.DoesNotContain(res, x => x.TimesheetCodeDefinitionId == cX.Id);
    }

    [Fact]
    public async Task GetActiveEntryOnDateAsync_returns_latest_by_From_when_multiple_overlap()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var tl = NewTimeline(personId, new DateOnly(2026, 1, 1));

        var c30 = NewCode("30");
        var cVp = NewCode("ВП");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(c30, cVp);
            db.TimesheetTimelines.Add(tl);

            db.TimesheetEntries.AddRange(
                NewEntry(tl.Id, personId, c30.Id, new DateOnly(2026, 1, 1), null),
                NewEntry(tl.Id, personId, cVp.Id, new DateOnly(2026, 1, 10), null)
            );

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var active = await repo.GetActiveEntryOnDateAsync(tl.Id, personId, new DateOnly(2026, 1, 15));

        Assert.NotNull(active);
        Assert.Equal(cVp.Id, active!.TimesheetCodeDefinitionId);
        Assert.Equal(new DateOnly(2026, 1, 10), active.From);

        // якщо репо робить Include — можна також перевірити код
        if (active.TimesheetCodeDefinition is not null)
            Assert.Equal("ВП", active.TimesheetCodeDefinition.Code);
    }

    [Fact]
    public async Task SoftDeleteAsync_marks_deleted_and_excludes_from_queries()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var tl = NewTimeline(personId, new DateOnly(2026, 1, 1));
        TimesheetEntry e;

        var c30 = NewCode("30");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(c30);
            db.TimesheetTimelines.Add(tl);

            e = NewEntry(tl.Id, personId, c30.Id, new DateOnly(2026, 1, 1), null);
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

        var tl1 = NewTimeline(p1, new DateOnly(2026, 1, 1));
        var tl2 = NewTimeline(p2, new DateOnly(2026, 1, 1));

        var c30 = NewCode("30");
        var cRozpor = NewCode("РОЗПОР");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(c30, cRozpor);
            db.TimesheetTimelines.AddRange(tl1, tl2);

            db.TimesheetEntries.AddRange(
                NewEntry(tl1.Id, p1, c30.Id, new DateOnly(2026, 1, 1), null),
                NewEntry(tl2.Id, p2, cRozpor.Id, new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 20))
            );

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var res = await repo.GetEntriesForPersonsAsync(
            personIds: [p1, p2],
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 1, 31));

        Assert.Equal(2, res.Count);
        Assert.Contains(res, x => x.PersonId == p1 && x.TimesheetCodeDefinitionId == c30.Id);
        Assert.Contains(res, x => x.PersonId == p2 && x.TimesheetCodeDefinitionId == cRozpor.Id);
    }

    [Fact]
    public async Task SaveTransitionAsync_updates_prev_and_adds_next_in_one_transaction()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var tl = NewTimeline(personId, new DateOnly(2026, 1, 1));

        TimesheetEntry prev;
        TimesheetEntry next;

        var c30 = NewCode("30");
        var c100 = NewCode("100");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(c30, c100);
            db.TimesheetTimelines.Add(tl);

            prev = NewEntry(tl.Id, personId, c30.Id, new DateOnly(2026, 1, 1), null);
            db.TimesheetEntries.Add(prev);

            await db.SaveChangesAsync();
        }

        // emulate transition: close prev + add next
        prev.To = new DateOnly(2026, 1, 9);
        prev.UpdatedBy = "ui";
        prev.UpdatedAtUtc = NowUtc;

        next = NewEntry(tl.Id, personId, c100.Id, new DateOnly(2026, 1, 10), null);
        next.CreatedBy = "ui";
        next.CreatedAtUtc = NowUtc;

        var repo = new TimesheetEntryRepository(tdb.Factory);
        await repo.SaveTransitionAsync(prev, next);

        await using (var db = await tdb.Factory.CreateDbContextAsync())
        {
            var all = await db.TimesheetEntries.AsNoTracking()
                .Where(x => x.PersonId == personId && !x.IsDeleted)
                .OrderBy(x => x.From)
                .ToListAsync();

            Assert.Equal(2, all.Count);

            Assert.Equal(c30.Id, all[0].TimesheetCodeDefinitionId);
            Assert.Equal(new DateOnly(2026, 1, 9), all[0].To);

            Assert.Equal(c100.Id, all[1].TimesheetCodeDefinitionId);
            Assert.Equal(new DateOnly(2026, 1, 10), all[1].From);
        }
    }

    // ----------------------------------------------------------------------
    // NEW: repository invariants (p.1 / p.2)
    // ----------------------------------------------------------------------

    [Fact]
    public async Task AddAsync_throws_when_timeline_closed()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var tl = NewTimeline(personId, openedAt: new DateOnly(2026, 1, 1), closedAt: new DateOnly(2026, 1, 5));

        var c30 = NewCode("30");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(c30);
            db.TimesheetTimelines.Add(tl);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);
        var entry = NewEntry(tl.Id, personId, c30.Id, new DateOnly(2026, 1, 2), null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.AddAsync(entry));
    }

    [Fact]
    public async Task SaveTransitionAsync_throws_when_timeline_closed()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var tl = NewTimeline(personId, openedAt: new DateOnly(2026, 1, 1), closedAt: new DateOnly(2026, 1, 5));

        TimesheetEntry prev;

        var c30 = NewCode("30");
        var c100 = NewCode("100");

        // Seed existing data directly (represents history before timeline got closed)
        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(c30, c100);
            db.TimesheetTimelines.Add(tl);

            prev = NewEntry(tl.Id, personId, c30.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5));
            db.TimesheetEntries.Add(prev);

            await db.SaveChangesAsync();
        }

        prev.To = new DateOnly(2026, 1, 5);
        prev.UpdatedBy = "ui";
        prev.UpdatedAtUtc = NowUtc;

        var next = NewEntry(tl.Id, personId, c100.Id, new DateOnly(2026, 1, 6), null);
        next.CreatedBy = "ui";
        next.CreatedAtUtc = NowUtc;

        var repo = new TimesheetEntryRepository(tdb.Factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SaveTransitionAsync(prev, next));
    }

    [Fact]
    public async Task AddAsync_throws_when_entry_starts_before_openedAt()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var tl = NewTimeline(personId, openedAt: new DateOnly(2026, 1, 10));

        var c30 = NewCode("30");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(c30);
            db.TimesheetTimelines.Add(tl);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var entry = NewEntry(tl.Id, personId, c30.Id, from: new DateOnly(2026, 1, 9), to: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.AddAsync(entry));
    }

    [Fact]
    public async Task AddAsync_throws_when_To_before_From()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var tl = NewTimeline(personId, openedAt: new DateOnly(2026, 1, 1));

        var c30 = NewCode("30");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(c30);
            db.TimesheetTimelines.Add(tl);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var entry = NewEntry(tl.Id, personId, c30.Id, from: new DateOnly(2026, 1, 10), to: new DateOnly(2026, 1, 9));

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.AddAsync(entry));
    }

    [Fact]
    public async Task UpdateAsync_throws_when_open_ended_in_closed_timeline()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var tl = NewTimeline(personId, openedAt: new DateOnly(2026, 1, 1), closedAt: new DateOnly(2026, 1, 5));

        TimesheetEntry e;

        var c30 = NewCode("30");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(c30);
            db.TimesheetTimelines.Add(tl);

            // Seed an entry that violates closed-timeline invariant (open-ended).
            // UpdateAsync should reject it (guard for data integrity).
            e = NewEntry(tl.Id, personId, c30.Id, new DateOnly(2026, 1, 1), to: null);
            db.TimesheetEntries.Add(e);

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        e.Note = "try update";

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.UpdateAsync(e));
    }

    [Fact]
    public async Task SaveTransitionAsync_throws_when_next_in_other_timeline()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var tl1 = NewTimeline(personId, new DateOnly(2026, 1, 1));

        TimesheetEntry prev;

        var c30 = NewCode("30");
        var c100 = NewCode("100");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(c30, c100);
            db.TimesheetTimelines.Add(tl1);

            prev = NewEntry(tl1.Id, personId, c30.Id, new DateOnly(2026, 1, 1), null);
            db.TimesheetEntries.Add(prev);

            await db.SaveChangesAsync();
        }

        prev.To = new DateOnly(2026, 1, 9);
        prev.UpdatedBy = "ui";
        prev.UpdatedAtUtc = NowUtc;

        // next "в іншому timeline" — НЕ потрібно створювати 2-й timeline в БД
        var next = NewEntry(Guid.NewGuid(), personId, c100.Id, new DateOnly(2026, 1, 10), null);

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SaveTransitionAsync(prev, next));
        Assert.Contains("одного таймлайну", ex.Message);
    }
}
