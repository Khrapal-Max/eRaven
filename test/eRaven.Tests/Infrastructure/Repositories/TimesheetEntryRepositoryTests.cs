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

/// <summary>
/// Тести для <see cref="TimesheetEntryRepository"/>.
///
/// <para>
/// Фіксуємо контракт CRUD/transition для <see cref="TimesheetEntry"/>:
/// <list type="bullet">
/// <item><description>read-методи ігнорують soft-delete;</description></item>
/// <item><description>overlap для entry інклюзивний: [From..To], To==null => open-ended;</description></item>
/// <item><description>GetActiveEntryOnDateAsync включає TimesheetCodeDefinition і повертає “найсвіжіший” запис;</description></item>
/// <item><description>Update/Transition заборонені у закритому епізоді;</description></item>
/// <item><description>Update/Transition валідують межі епізоду (OpenedAt..ClosedAt).</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class TimesheetEntryRepositoryTests
{
    //======================================================================
    // Reads
    //======================================================================

    /// <summary>
    /// GetByIdAsync повертає entry лише якщо він не soft-deleted.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForSoftDeleted()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();

        var codeId = await SeedCodeAsync(testDb, "T");
        var tsId = await SeedEpisodeAsync(testDb, personId, openedAt: new DateOnly(2026, 02, 01), closedAt: null, nowUtc: now);

        var entryId = Guid.NewGuid();
        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = entryId,
            TimesheetId = tsId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeId,
            From = new DateOnly(2026, 02, 01),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = now
        });

        // visible
        var e1 = await repo.GetByIdAsync(entryId);
        Assert.NotNull(e1);

        // soft delete
        await repo.SoftDeleteAsync(entryId, reason: "  test  ", author: "  user  ", nowUtc: now.AddMinutes(1));

        // hidden
        var e2 = await repo.GetByIdAsync(entryId);
        Assert.Null(e2);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var raw = await db.TimesheetEntries.SingleAsync(x => x.Id == entryId);
        Assert.True(raw.IsDeleted);
        Assert.Equal("user", raw.DeletedBy);
        Assert.Equal("test", raw.DeleteReason);
    }

    /// <summary>
    /// GetNextEntryAfterDateAsync повертає найближчий наступний entry по From (та Id як tie-breaker).
    /// </summary>
    [Fact]
    public async Task GetNextEntryAfterDateAsync_ReturnsNextByFrom()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();

        var codeId = await SeedCodeAsync(testDb, "T");
        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, now);

        var e1 = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeId,
            From = new DateOnly(2026, 02, 05),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = now
        };
        var e2 = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeId,
            From = new DateOnly(2026, 02, 07),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = now
        };

        await SeedEntryAsync(testDb, e2);
        await SeedEntryAsync(testDb, e1);

        var next = await repo.GetNextEntryAfterDateAsync(tsId, personId, new DateOnly(2026, 02, 05));
        Assert.NotNull(next);
        Assert.Equal(new DateOnly(2026, 02, 07), next!.From);
    }

    /// <summary>
    /// GetPersonEntriesAsync повертає лише ті записи, що перетинаються з [from..to] (інклюзивно),
    /// та ігнорує soft-delete.
    /// </summary>
    [Fact]
    public async Task GetPersonEntriesAsync_ReturnsOverlapping_AndIgnoresDeleted()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();

        var codeT = await SeedCodeAsync(testDb, "T");
        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, now);

        // [02-01..02-03]
        var a = NewEntry(tsId, personId, codeT, new DateOnly(2026, 02, 01), new DateOnly(2026, 02, 03), now, "a");
        // [02-04..null]
        var b = NewEntry(tsId, personId, codeT, new DateOnly(2026, 02, 04), null, now, "b");
        // [01-20..01-31] (no overlap)
        var c = NewEntry(tsId, personId, codeT, new DateOnly(2026, 01, 20), new DateOnly(2026, 01, 31), now, "c");

        await SeedEntryAsync(testDb, a);
        await SeedEntryAsync(testDb, b);
        await SeedEntryAsync(testDb, c);

        // soft-delete b
        await repo.SoftDeleteAsync(b.Id, reason: "x", author: "u", nowUtc: now.AddMinutes(1));

        var list = await repo.GetPersonEntriesAsync(personId, from: new DateOnly(2026, 02, 02), to: new DateOnly(2026, 02, 05));

        // overlap window [02-02..02-05] intersects 'a' (02-02..02-03), but 'b' is deleted
        Assert.Single(list);
        Assert.Equal(a.Id, list[0].Id);
    }

    /// <summary>
    /// GetEntriesForPersonsAsync повертає записи для багатьох осіб з overlap та сортуванням,
    /// а для порожнього списку повертає [].
    /// </summary>
    [Fact]
    public async Task GetEntriesForPersonsAsync_ReturnsForManyPersons_AndEmptyForEmptyIds()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);

        var codeT = await SeedCodeAsync(testDb, "T");

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var ts1 = await SeedEpisodeAsync(testDb, p1, new DateOnly(2026, 02, 01), null, now);
        var ts2 = await SeedEpisodeAsync(testDb, p2, new DateOnly(2026, 02, 01), null, now);

        var e11 = NewEntry(ts1, p1, codeT, new DateOnly(2026, 02, 01), null, now, "p1");
        var e21 = NewEntry(ts2, p2, codeT, new DateOnly(2026, 02, 02), null, now, "p2");

        await SeedEntryAsync(testDb, e11);
        await SeedEntryAsync(testDb, e21);

        var empty = await repo.GetEntriesForPersonsAsync(Array.Empty<Guid>(), new DateOnly(2026, 02, 01), new DateOnly(2026, 02, 10));
        Assert.Empty(empty);

        var list = await repo.GetEntriesForPersonsAsync(new[] { p2, p1 }, new DateOnly(2026, 02, 01), new DateOnly(2026, 02, 10));

        Assert.Equal(2, list.Count);

        // sorted by PersonId, then From
        Assert.True(list[0].PersonId.CompareTo(list[1].PersonId) <= 0);
    }

    /// <summary>
    /// GetActiveEntryOnDateAsync повертає “найсвіжіший” entry (max From), який покриває дату,
    /// та включає TimesheetCodeDefinition.
    /// </summary>
    [Fact]
    public async Task GetActiveEntryOnDateAsync_ReturnsLatestCovering_AndIncludesCode()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();

        var codeT = await SeedCodeAsync(testDb, "T");
        var code30 = await SeedCodeAsync(testDb, "30");

        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, now);

        // base [02-01..null] code T
        var baseEntry = NewEntry(tsId, personId, codeT, new DateOnly(2026, 02, 01), null, now, "base");
        // override [02-05..null] code 30
        var overrideEntry = NewEntry(tsId, personId, code30, new DateOnly(2026, 02, 05), null, now, "ovr");

        await SeedEntryAsync(testDb, baseEntry);
        await SeedEntryAsync(testDb, overrideEntry);

        var active = await repo.GetActiveEntryOnDateAsync(tsId, personId, new DateOnly(2026, 02, 10));

        Assert.NotNull(active);
        Assert.Equal(overrideEntry.Id, active!.Id);
        Assert.Equal(new DateOnly(2026, 02, 05), active.From);

        // Include(TimesheetCodeDefinition)
        Assert.NotNull(active.TimesheetCodeDefinition);
        Assert.Equal("30", active.TimesheetCodeDefinition!.Code);
    }

    //======================================================================
    // SoftDelete
    //======================================================================

    /// <summary>
    /// SoftDeleteAsync є no-op для неіснуючого/вже видаленого запису.
    /// </summary>
    [Fact]
    public async Task SoftDeleteAsync_NoOp_WhenMissingOrAlreadyDeleted()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);

        // missing -> no throw
        await repo.SoftDeleteAsync(Guid.NewGuid(), "r", "u", now);

        var personId = Guid.NewGuid();
        var codeId = await SeedCodeAsync(testDb, "T");
        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, now);

        var entry = NewEntry(tsId, personId, codeId, new DateOnly(2026, 02, 01), null, now, "x");
        await SeedEntryAsync(testDb, entry);

        await repo.SoftDeleteAsync(entry.Id, "r", "u", now.AddMinutes(1));
        await repo.SoftDeleteAsync(entry.Id, "r2", "u2", now.AddMinutes(2)); // no overwrite expected

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var raw = await db.TimesheetEntries.SingleAsync(x => x.Id == entry.Id);

        Assert.True(raw.IsDeleted);
        Assert.Equal("u", raw.DeletedBy);
        Assert.Equal("r", raw.DeleteReason);
        Assert.Equal(now.AddMinutes(1), raw.DeletedAtUtc);
    }

    /// <summary>
    /// SoftDeleteAsync підставляє DeletedBy="system", якщо author порожній.
    /// </summary>
    [Fact]
    public async Task SoftDeleteAsync_UsesSystem_WhenAuthorEmpty()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);

        var personId = Guid.NewGuid();
        var codeId = await SeedCodeAsync(testDb, "T");
        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, now);

        var entry = NewEntry(tsId, personId, codeId, new DateOnly(2026, 02, 01), null, now, "x");
        await SeedEntryAsync(testDb, entry);

        await repo.SoftDeleteAsync(entry.Id, "r", "   ", now.AddMinutes(1));

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var raw = await db.TimesheetEntries.SingleAsync(x => x.Id == entry.Id);

        Assert.True(raw.IsDeleted);
        Assert.Equal("system", raw.DeletedBy);
    }

    //======================================================================
    // UpdateAsync
    //======================================================================

    /// <summary>
    /// UpdateAsync оновлює існуючий запис (in-place), якщо він в межах відкритого епізоду.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_UpdatesExistingEntry_WhenWithinOpenEpisode()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(testDb.Factory);

        var t0 = Utc(2026, 02, 17, 10, 00);
        var t1 = t0.AddMinutes(5);

        var personId = Guid.NewGuid();
        var codeId = await SeedCodeAsync(testDb, "T");
        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, t0);

        var entry = NewEntry(tsId, personId, codeId, new DateOnly(2026, 02, 01), null, t0, "x");
        await SeedEntryAsync(testDb, entry);

        // load (detached), modify, update
        var updated = await LoadEntryAsync(testDb, entry.Id);
        updated.To = new DateOnly(2026, 02, 09);
        updated.UpdatedBy = "editor";
        updated.UpdatedAtUtc = t1;

        await repo.UpdateAsync(updated);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var raw = await db.TimesheetEntries.SingleAsync(x => x.Id == entry.Id);

        Assert.Equal(new DateOnly(2026, 02, 09), raw.To);
        Assert.Equal("editor", raw.UpdatedBy);
        Assert.Equal(t1, raw.UpdatedAtUtc);
    }

    /// <summary>
    /// UpdateAsync кидає, якщо епізод закритий (ClosedAt != null).
    /// </summary>
    [Fact]
    public async Task UpdateAsync_Throws_WhenEpisodeClosed()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);

        var personId = Guid.NewGuid();
        var codeId = await SeedCodeAsync(testDb, "T");

        var tsId = await SeedEpisodeAsync(
            testDb,
            personId,
            openedAt: new DateOnly(2026, 02, 01),
            closedAt: new DateOnly(2026, 02, 10),
            nowUtc: now);

        var entry = NewEntry(tsId, personId, codeId, new DateOnly(2026, 02, 01), new DateOnly(2026, 02, 10), now, "x");
        await SeedEntryAsync(testDb, entry);

        var updated = await LoadEntryAsync(testDb, entry.Id);
        updated.Note = "changed";

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.UpdateAsync(updated));
        Assert.Contains("закритий", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// UpdateAsync валідовує межі епізоду: From не може бути раніше OpenedAt, To не може бути < From,
    /// PersonId і TimesheetId мають відповідати власнику епізоду.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_Throws_WhenEntryViolatesEpisodeBounds()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);

        var personId = Guid.NewGuid();
        var codeId = await SeedCodeAsync(testDb, "T");
        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 05), null, now);

        // create an entry that exists
        var entry = NewEntry(tsId, personId, codeId, new DateOnly(2026, 02, 05), null, now, "x");
        await SeedEntryAsync(testDb, entry);

        // 1) From < OpenedAt
        var bad1 = await LoadEntryAsync(testDb, entry.Id);
        bad1.From = new DateOnly(2026, 02, 01);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.UpdateAsync(bad1));

        // 2) To < From
        var bad2 = await LoadEntryAsync(testDb, entry.Id);
        bad2.To = new DateOnly(2026, 02, 04);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.UpdateAsync(bad2));

        // 3) PersonId mismatch
        var bad3 = await LoadEntryAsync(testDb, entry.Id);
        bad3.PersonId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.UpdateAsync(bad3));
    }

    //======================================================================
    // SaveTransitionAsync
    //======================================================================

    /// <summary>
    /// SaveTransitionAsync атомарно:
    /// <list type="bullet">
    /// <item><description>оновлює prev (закриває To/Updated*);</description></item>
    /// <item><description>додає next (From..To);</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task SaveTransitionAsync_UpdatesPrev_AndAddsNext()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(testDb.Factory);

        var t0 = Utc(2026, 02, 17, 10, 00);
        var t1 = t0.AddMinutes(10);

        var personId = Guid.NewGuid();

        var codeT = await SeedCodeAsync(testDb, "T");
        var code30 = await SeedCodeAsync(testDb, "30");

        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, t0);

        var prev = NewEntry(tsId, personId, codeT, new DateOnly(2026, 02, 01), null, t0, "prev");
        await SeedEntryAsync(testDb, prev);

        // transition at 2026-02-10:
        // prev becomes [02-01..02-09]
        // next becomes [02-10..null]
        var prevUpdated = await LoadEntryAsync(testDb, prev.Id);
        prevUpdated.To = new DateOnly(2026, 02, 09);
        prevUpdated.UpdatedBy = "duty";
        prevUpdated.UpdatedAtUtc = t1;

        var nextAdded = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsId,
            PersonId = personId,
            TimesheetCodeDefinitionId = code30,
            From = new DateOnly(2026, 02, 10),
            To = null,
            Reference = "ORD-1",
            Note = "note",
            CreatedBy = "duty",
            CreatedAtUtc = t1
        };

        await repo.SaveTransitionAsync(prevUpdated, nextAdded);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        var all = await db.TimesheetEntries
            .Where(x => x.TimesheetId == tsId)
            .OrderBy(x => x.From)
            .ToListAsync();

        Assert.Equal(2, all.Count);

        Assert.Equal(new DateOnly(2026, 02, 01), all[0].From);
        Assert.Equal(new DateOnly(2026, 02, 09), all[0].To);
        Assert.Equal("duty", all[0].UpdatedBy);

        Assert.Equal(new DateOnly(2026, 02, 10), all[1].From);
        Assert.Null(all[1].To);
        Assert.Equal("ORD-1", all[1].Reference);
        Assert.Equal("note", all[1].Note);
    }

    /// <summary>
    /// SaveTransitionAsync кидає, якщо prev/next в різних TimesheetId.
    /// </summary>
    [Fact]
    public async Task SaveTransitionAsync_Throws_WhenTimesheetMismatch()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);

        var personId = Guid.NewGuid();
        var codeT = await SeedCodeAsync(testDb, "T");
        var ts1 = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, now);
        var ts2 = await SeedEpisodeAsync(testDb, Guid.NewGuid(), new DateOnly(2026, 02, 01), null, now);

        var prev = NewEntry(ts1, personId, codeT, new DateOnly(2026, 02, 01), null, now, "prev");
        await SeedEntryAsync(testDb, prev);

        var prevUpdated = await LoadEntryAsync(testDb, prev.Id);
        prevUpdated.To = new DateOnly(2026, 02, 05);

        var next = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = ts2,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeT,
            From = new DateOnly(2026, 02, 06),
            To = null,
            CreatedBy = "x",
            CreatedAtUtc = now
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SaveTransitionAsync(prevUpdated, next));
    }

    /// <summary>
    /// SaveTransitionAsync заборонений у закритому епізоді.
    /// </summary>
    [Fact]
    public async Task SaveTransitionAsync_Throws_WhenEpisodeClosed()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);

        var personId = Guid.NewGuid();
        var codeT = await SeedCodeAsync(testDb, "T");

        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), new DateOnly(2026, 02, 10), now);

        var prev = NewEntry(tsId, personId, codeT, new DateOnly(2026, 02, 01), new DateOnly(2026, 02, 10), now, "prev");
        await SeedEntryAsync(testDb, prev);

        var prevUpdated = await LoadEntryAsync(testDb, prev.Id);
        prevUpdated.To = new DateOnly(2026, 02, 09);

        var next = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeT,
            From = new DateOnly(2026, 02, 10),
            To = new DateOnly(2026, 02, 10),
            CreatedBy = "x",
            CreatedAtUtc = now
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SaveTransitionAsync(prevUpdated, next));
        Assert.Contains("закритий", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static DateTime Utc(int y, int m, int d, int hh, int mm)
        => new(y, m, d, hh, mm, 0, DateTimeKind.Utc);

    /// <summary>
    /// Сідить TimesheetCodeDefinition з унікальним Code.
    /// </summary>
    private static async Task<Guid> SeedCodeAsync(SqliteTestDb testDb, string code)
    {
        await using var db = await testDb.Factory.CreateDbContextAsync();

        var existing = await db.TimesheetCodes.SingleOrDefaultAsync(x => x.Code == code);
        if (existing is not null)
            return existing.Id;

        var e = new TimesheetCodeDefinition
        {
            Id = Guid.NewGuid(),
            Code = code,
            Title = code,
            Description = null,
            SortOrder = 0,
            Priority = 0,
            IsTerminal = false,
            IsActive = true,
            CreatedBy = "seed",
            CreatedAtUtc = Utc(2026, 02, 17, 10, 00)
        };

        db.TimesheetCodes.Add(e);
        await db.SaveChangesAsync();

        return e.Id;
    }

    /// <summary>
    /// Сідить епізод табеля.
    /// </summary>
    private static async Task<Guid> SeedEpisodeAsync(
        SqliteTestDb testDb,
        Guid personId,
        DateOnly openedAt,
        DateOnly? closedAt,
        DateTime nowUtc)
    {
        var ep = new TimeSheetAggregate
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            OpenedAt = openedAt,
            ClosedAt = closedAt,
            CreatedBy = "seed",
            CreatedAtUtc = nowUtc
        };

        await using var db = await testDb.Factory.CreateDbContextAsync();
        db.TimeSheets.Add(ep);
        await db.SaveChangesAsync();

        return ep.Id;
    }

    /// <summary>
    /// Додає entry напряму в БД (поза репозиторієм).
    /// </summary>
    private static async Task SeedEntryAsync(SqliteTestDb testDb, TimesheetEntry entry)
    {
        await using var db = await testDb.Factory.CreateDbContextAsync();
        db.TimesheetEntries.Add(entry);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Завантажує entry як detached (через новий DbContext).
    /// </summary>
    private static async Task<TimesheetEntry> LoadEntryAsync(SqliteTestDb testDb, Guid entryId)
    {
        await using var db = await testDb.Factory.CreateDbContextAsync();
        return await db.TimesheetEntries.AsNoTracking().SingleAsync(x => x.Id == entryId);
    }

    private static TimesheetEntry NewEntry(
        Guid timesheetId,
        Guid personId,
        Guid codeId,
        DateOnly from,
        DateOnly? to,
        DateTime createdAtUtc,
        string marker)
        => new()
        {
            Id = Guid.NewGuid(),
            TimesheetId = timesheetId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeId,
            From = from,
            To = to,
            Reference = marker,
            Note = null,
            CreatedBy = "seed",
            CreatedAtUtc = createdAtUtc,
            IsDeleted = false
        };
}
