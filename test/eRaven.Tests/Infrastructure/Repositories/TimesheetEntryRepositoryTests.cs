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
    private static readonly DateTime NowUtc = new(2026, 02, 15, 12, 00, 00, DateTimeKind.Utc);

    //======================================================================
    // Helpers
    //======================================================================

    private static TimesheetCodeDefinition NewCode(Guid id, string code = "30")
        => new()
        {
            Id = id,
            Code = code,
            Title = "Ready",
            SortOrder = 1,
            Priority = 1,
            IsTerminal = false,
            IsActive = true,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc.AddHours(-1)
        };

    private static TimeSheetAggregate NewEpisode(Guid id, Guid personId, DateOnly openedAt, DateOnly? closedAt = null)
        => new()
        {
            Id = id,
            PersonId = personId,
            OpenedAt = openedAt,
            ClosedAt = closedAt,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc.AddHours(-2),
            ClosedBy = null,
            ClosedAtUtc = null
        };

    private static TimesheetEntry NewEntry(
        Guid id,
        Guid timesheetId,
        Guid personId,
        Guid codeId,
        DateOnly from,
        DateOnly? to = null,
        bool isDeleted = false)
        => new()
        {
            Id = id,
            TimesheetId = timesheetId,
            TimeSheet = null,

            PersonId = personId,

            TimesheetCodeDefinitionId = codeId,
            TimesheetCodeDefinition = null,

            From = from,
            To = to,

            Reference = null,
            Note = null,

            CreatedBy = "seed",
            CreatedAtUtc = NowUtc.AddHours(-1),

            UpdatedBy = null,
            UpdatedAtUtc = null,

            IsDeleted = isDeleted,
            DeletedBy = null,
            DeletedAtUtc = null,
            DeleteReason = null
        };

    //======================================================================
    // GetByIdAsync
    //======================================================================

    [Fact]
    public async Task GetByIdAsync_returns_null_when_not_found()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(tdb.Factory);

        var found = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task GetByIdAsync_ignores_soft_deleted()
    {
        await using var tdb = new SqliteTestDb();

        var codeId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId));
            db.TimeSheets.Add(NewEpisode(episodeId, personId, new DateOnly(2026, 2, 1)));

            db.TimesheetEntries.Add(NewEntry(entryId, episodeId, personId, codeId, new DateOnly(2026, 2, 1), isDeleted: true));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var found = await repo.GetByIdAsync(entryId);

        Assert.Null(found);
    }

    [Fact]
    public async Task GetByIdAsync_returns_entry_when_exists_and_not_deleted()
    {
        await using var tdb = new SqliteTestDb();

        var codeId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId));
            db.TimeSheets.Add(NewEpisode(episodeId, personId, new DateOnly(2026, 2, 1)));

            db.TimesheetEntries.Add(NewEntry(entryId, episodeId, personId, codeId, new DateOnly(2026, 2, 1)));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var found = await repo.GetByIdAsync(entryId);

        Assert.NotNull(found);
        Assert.Equal(entryId, found!.Id);
        Assert.False(found.IsDeleted);
    }

    //======================================================================
    // GetNextEntryAfterDateAsync
    //======================================================================

    [Fact]
    public async Task GetNextEntryAfterDateAsync_returns_first_entry_after_date_sorted_by_from_then_id()
    {
        await using var tdb = new SqliteTestDb();

        var codeId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();

        var id1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var id2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var id3 = Guid.Parse("00000000-0000-0000-0000-000000000003");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId));
            db.TimeSheets.Add(NewEpisode(episodeId, personId, new DateOnly(2026, 2, 1)));

            // From == date => НЕ підходить (умова From > date)
            db.TimesheetEntries.Add(NewEntry(id1, episodeId, personId, codeId, new DateOnly(2026, 2, 10)));

            // два кандидати From=2026-02-11 -> вибираємо менший Id (ThenBy(x => x.Id))
            db.TimesheetEntries.Add(NewEntry(id3, episodeId, personId, codeId, new DateOnly(2026, 2, 11)));
            db.TimesheetEntries.Add(NewEntry(id2, episodeId, personId, codeId, new DateOnly(2026, 2, 11)));

            // deleted — ігноруємо
            db.TimesheetEntries.Add(NewEntry(Guid.NewGuid(), episodeId, personId, codeId, new DateOnly(2026, 2, 11), isDeleted: true));

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var next = await repo.GetNextEntryAfterDateAsync(episodeId, personId, new DateOnly(2026, 2, 10));

        Assert.NotNull(next);
        Assert.Equal(new DateOnly(2026, 2, 11), next!.From);
        Assert.Equal(id2, next.Id); // smallest id on same From
    }

    //======================================================================
    // GetPersonEntriesAsync
    //======================================================================

    [Fact]
    public async Task GetPersonEntriesAsync_throws_when_to_before_from()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetPersonEntriesAsync(Guid.NewGuid(), new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 9)));
    }

    [Fact]
    public async Task GetPersonEntriesAsync_returns_only_overlapping_and_sorted_and_ignores_deleted()
    {
        await using var tdb = new SqliteTestDb();

        var codeId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var otherPersonId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();

        var e0 = NewEntry(Guid.NewGuid(), episodeId, personId, codeId, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 5)); // outside window
        var e1 = NewEntry(Guid.NewGuid(), episodeId, personId, codeId, new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 12)); // overlap
        var e2 = NewEntry(Guid.NewGuid(), episodeId, personId, codeId, new DateOnly(2026, 2, 15), null); // overlap
        var eDel = NewEntry(Guid.NewGuid(), episodeId, personId, codeId, new DateOnly(2026, 2, 11), null, isDeleted: true); // ignore
        var eOther = NewEntry(Guid.NewGuid(), episodeId, otherPersonId, codeId, new DateOnly(2026, 2, 10), null); // other person

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId));
            db.TimeSheets.Add(NewEpisode(episodeId, personId, new DateOnly(2026, 2, 1)));
            db.TimesheetEntries.AddRange(e0, e1, e2, eDel, eOther);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var list = await repo.GetPersonEntriesAsync(personId, new DateOnly(2026, 2, 11), new DateOnly(2026, 2, 20));

        Assert.Equal(2, list.Count);
        Assert.Equal(e1.Id, list[0].Id);
        Assert.Equal(e2.Id, list[1].Id);
    }

    //======================================================================
    // GetEntriesForPersonsAsync
    //======================================================================

    [Fact]
    public async Task GetEntriesForPersonsAsync_returns_empty_when_personIds_empty()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(tdb.Factory);

        var list = await repo.GetEntriesForPersonsAsync([], new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 2));

        Assert.Empty(list);
    }

    [Fact]
    public async Task GetEntriesForPersonsAsync_throws_when_to_before_from()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetEntriesForPersonsAsync([Guid.NewGuid()], new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 9)));
    }

    [Fact]
    public async Task GetEntriesForPersonsAsync_returns_overlapping_for_many_persons_sorted_by_person_then_from_then_id()
    {
        await using var tdb = new SqliteTestDb();

        var codeId = Guid.NewGuid();
        var p1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var p2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var episode1 = Guid.NewGuid();
        var episode2 = Guid.NewGuid();

        var idA = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var idB = Guid.Parse("00000000-0000-0000-0000-000000000011");
        var idC = Guid.Parse("00000000-0000-0000-0000-000000000012");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId));
            db.TimeSheets.AddRange(
                NewEpisode(episode1, p1, new DateOnly(2026, 2, 1)),
                NewEpisode(episode2, p2, new DateOnly(2026, 2, 1)));

            // p2 first by From (later), but overall ordering by PersonId => p1 results first
            db.TimesheetEntries.AddRange(
                NewEntry(idB, episode2, p2, codeId, new DateOnly(2026, 2, 12), null), // p2
                NewEntry(idA, episode1, p1, codeId, new DateOnly(2026, 2, 11), null), // p1 (earlier)
                NewEntry(idC, episode1, p1, codeId, new DateOnly(2026, 2, 12), null)  // p1 (later)
            );

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var list = await repo.GetEntriesForPersonsAsync([p2, p1], new DateOnly(2026, 2, 11), new DateOnly(2026, 2, 20));

        Assert.Equal(3, list.Count);
        Assert.Equal(p1, list[0].PersonId);
        Assert.Equal(idA, list[0].Id);
        Assert.Equal(idC, list[1].Id);
        Assert.Equal(p2, list[2].PersonId);
        Assert.Equal(idB, list[2].Id);
    }

    //======================================================================
    // GetActiveEntryOnDateAsync
    //======================================================================

    [Fact]
    public async Task GetActiveEntryOnDateAsync_returns_latest_covering_date_and_includes_code_definition()
    {
        await using var tdb = new SqliteTestDb();

        var codeId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();

        var idSmall = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var idBig = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId, "30"));
            db.TimeSheets.Add(NewEpisode(episodeId, personId, new DateOnly(2026, 2, 1)));

            // same From, both open-ended and cover date => tie-break by Id desc
            db.TimesheetEntries.Add(NewEntry(idSmall, episodeId, personId, codeId, new DateOnly(2026, 2, 10), null));
            db.TimesheetEntries.Add(NewEntry(idBig, episodeId, personId, codeId, new DateOnly(2026, 2, 10), null));

            // deleted active — ignore
            db.TimesheetEntries.Add(NewEntry(Guid.NewGuid(), episodeId, personId, codeId, new DateOnly(2026, 2, 9), null, isDeleted: true));

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var active = await repo.GetActiveEntryOnDateAsync(episodeId, personId, new DateOnly(2026, 2, 11));

        Assert.NotNull(active);
        Assert.Equal(idBig, active!.Id);
        Assert.NotNull(active.TimesheetCodeDefinition);
        Assert.Equal(codeId, active.TimesheetCodeDefinition!.Id);
    }

    //======================================================================
    // SoftDeleteAsync
    //======================================================================

    [Fact]
    public async Task SoftDeleteAsync_throws_on_empty_entry_id()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.SoftDeleteAsync(Guid.Empty, reason: "r", author: "a", nowUtc: NowUtc));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SoftDeleteAsync_throws_on_empty_reason(string? reason)
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.SoftDeleteAsync(Guid.NewGuid(), reason: reason!, author: "a", nowUtc: NowUtc));
    }

    [Fact]
    public async Task SoftDeleteAsync_noops_when_not_found()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(tdb.Factory);

        await repo.SoftDeleteAsync(Guid.NewGuid(), reason: "r", author: "a", nowUtc: NowUtc);
        // no throw => ok
    }

    [Fact]
    public async Task SoftDeleteAsync_marks_deleted_and_sets_audit_fields_and_trims_values()
    {
        await using var tdb = new SqliteTestDb();

        var codeId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId));
            db.TimeSheets.Add(NewEpisode(episodeId, personId, new DateOnly(2026, 2, 1)));

            db.TimesheetEntries.Add(NewEntry(entryId, episodeId, personId, codeId, new DateOnly(2026, 2, 10)));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        await repo.SoftDeleteAsync(entryId, reason: "  bad data  ", author: "  user1  ", nowUtc: NowUtc);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.TimesheetEntries.AsNoTracking().SingleAsync(x => x.Id == entryId);

        Assert.True(reloaded.IsDeleted);
        Assert.Equal("user1", reloaded.DeletedBy);
        Assert.Equal(NowUtc, reloaded.DeletedAtUtc);
        Assert.Equal("bad data", reloaded.DeleteReason);
    }

    [Fact]
    public async Task SoftDeleteAsync_uses_system_when_author_blank_and_does_not_redelete()
    {
        await using var tdb = new SqliteTestDb();

        var codeId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId));
            db.TimeSheets.Add(NewEpisode(episodeId, personId, new DateOnly(2026, 2, 1)));

            var e = NewEntry(entryId, episodeId, personId, codeId, new DateOnly(2026, 2, 10));
            e.IsDeleted = true;
            e.DeletedBy = "seed";
            e.DeletedAtUtc = NowUtc.AddHours(-3);
            e.DeleteReason = "r1";

            db.TimesheetEntries.Add(e);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        await repo.SoftDeleteAsync(entryId, reason: "r2", author: "   ", nowUtc: NowUtc);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.TimesheetEntries.AsNoTracking().SingleAsync(x => x.Id == entryId);

        // already deleted => noop
        Assert.True(reloaded.IsDeleted);
        Assert.Equal("seed", reloaded.DeletedBy);
        Assert.Equal(NowUtc.AddHours(-3), reloaded.DeletedAtUtc);
        Assert.Equal("r1", reloaded.DeleteReason);
    }

    //======================================================================
    // UpdateAsync
    //======================================================================

    [Fact]
    public async Task UpdateAsync_throws_on_null_updated()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repo.UpdateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_throws_on_empty_entry_id()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEntryRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.UpdateAsync(new TimesheetEntry { Id = Guid.Empty }));
    }

    [Fact]
    public async Task UpdateAsync_throws_when_episode_closed()
    {
        await using var tdb = new SqliteTestDb();

        var codeId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId));
            db.TimeSheets.Add(NewEpisode(episodeId, personId, new DateOnly(2026, 2, 1), closedAt: new DateOnly(2026, 2, 10)));

            db.TimesheetEntries.Add(NewEntry(entryId, episodeId, personId, codeId, new DateOnly(2026, 2, 5)));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var updated = NewEntry(entryId, episodeId, personId, codeId, new DateOnly(2026, 2, 5), new DateOnly(2026, 2, 6));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.UpdateAsync(updated));

        Assert.Contains("Епізод табеля закритий", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_throws_when_entry_from_before_openedAt()
    {
        await using var tdb = new SqliteTestDb();

        var codeId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId));
            db.TimeSheets.Add(NewEpisode(episodeId, personId, new DateOnly(2026, 2, 10)));

            db.TimesheetEntries.Add(NewEntry(entryId, episodeId, personId, codeId, new DateOnly(2026, 2, 10)));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var updated = NewEntry(entryId, episodeId, personId, codeId, new DateOnly(2026, 2, 9));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.UpdateAsync(updated));

        Assert.Contains("Запис не може починатися раніше OpenedAt", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_throws_when_to_before_from()
    {
        await using var tdb = new SqliteTestDb();

        var codeId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId));
            db.TimeSheets.Add(NewEpisode(episodeId, personId, new DateOnly(2026, 2, 1)));
            db.TimesheetEntries.Add(NewEntry(entryId, episodeId, personId, codeId, new DateOnly(2026, 2, 10)));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var updated = NewEntry(entryId, episodeId, personId, codeId, new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 9));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.UpdateAsync(updated));

        Assert.Equal("Некоректний період: To не може бути раніше From.", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_persists_changes()
    {
        await using var tdb = new SqliteTestDb();

        var codeId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId));
            db.TimeSheets.Add(NewEpisode(episodeId, personId, new DateOnly(2026, 2, 1)));

            var e = NewEntry(entryId, episodeId, personId, codeId, new DateOnly(2026, 2, 10), null);
            e.Note = "old";
            db.TimesheetEntries.Add(e);

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        // detached updated entity
        var updated = NewEntry(entryId, episodeId, personId, codeId, new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 12));
        updated.Note = "new-note";

        await repo.UpdateAsync(updated);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.TimesheetEntries.AsNoTracking().SingleAsync(x => x.Id == entryId);

        Assert.Equal(new DateOnly(2026, 2, 12), reloaded.To);
        Assert.Equal("new-note", reloaded.Note);
    }

    //======================================================================
    // SaveTransitionAsync
    //======================================================================

    [Fact]
    public async Task SaveTransitionAsync_throws_when_timesheet_ids_different()
    {
        await using var tdb = new SqliteTestDb();

        var codeId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId));
            db.TimeSheets.Add(NewEpisode(episodeId, personId, new DateOnly(2026, 2, 1)));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var prev = NewEntry(Guid.NewGuid(), episodeId, personId, codeId, new DateOnly(2026, 2, 10), null);

        // інший TimesheetId, і він може навіть не існувати в БД — метод впаде раніше
        var otherEpisodeId = Guid.NewGuid();
        var next = NewEntry(Guid.NewGuid(), otherEpisodeId, personId, codeId, new DateOnly(2026, 2, 11), null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.SaveTransitionAsync(prev, next));

        Assert.Equal("Перехід повинен виконуватись в межах одного епізоду табеля.", ex.Message);
    }

    [Fact]
    public async Task SaveTransitionAsync_updates_prev_and_inserts_next_atomically()
    {
        await using var tdb = new SqliteTestDb();

        var code30Id = Guid.NewGuid();
        var code40Id = Guid.NewGuid();

        var personId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();

        var prevId = Guid.NewGuid();
        var nextId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(NewCode(code30Id, "30"), NewCode(code40Id, "40"));
            db.TimeSheets.Add(NewEpisode(episodeId, personId, new DateOnly(2026, 2, 1)));

            // existing prev entry
            db.TimesheetEntries.Add(NewEntry(prevId, episodeId, personId, code30Id, new DateOnly(2026, 2, 10), null));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEntryRepository(tdb.Factory);

        var prevUpdated = NewEntry(prevId, episodeId, personId, code30Id, new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 10));
        prevUpdated.Note = "closed";

        var nextAdded = NewEntry(nextId, episodeId, personId, code40Id, new DateOnly(2026, 2, 11), null);
        nextAdded.Note = "next";

        await repo.SaveTransitionAsync(prevUpdated, nextAdded);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();

        var prevReload = await db2.TimesheetEntries.AsNoTracking().SingleAsync(x => x.Id == prevId);
        Assert.Equal(new DateOnly(2026, 2, 10), prevReload.To);
        Assert.Equal("closed", prevReload.Note);

        var nextReload = await db2.TimesheetEntries.AsNoTracking().SingleAsync(x => x.Id == nextId);
        Assert.Equal(new DateOnly(2026, 2, 11), nextReload.From);
        Assert.Null(nextReload.To);
        Assert.Equal("next", nextReload.Note);
    }
}
