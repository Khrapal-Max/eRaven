//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEpisodeRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetEpisodeRepositoryTests
{
    private static readonly DateTime NowUtc = new(2026, 02, 15, 12, 00, 00, DateTimeKind.Utc);

    //======================================================================
    // Helpers
    //======================================================================

    private static TimesheetCodeDefinition NewCode(Guid id, string code, bool isActive = true)
        => new()
        {
            Id = id,
            Code = code,
            Title = code,
            SortOrder = 1,
            Priority = 1,
            IsTerminal = false,
            IsActive = isActive,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc.AddHours(-2)
        };

    private static TimeSheetAggregate NewEpisode(
        Guid id,
        Guid personId,
        DateOnly openedAt,
        DateOnly? closedAt = null)
        => new()
        {
            Id = id,
            PersonId = personId,
            OpenedAt = openedAt,
            ClosedAt = closedAt,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc.AddHours(-3),
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
        bool isDeleted = false,
        string? reference = null)
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

            Reference = reference,
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

    private static TimesheetTaskSpan NewSpan(
        Guid id,
        Guid timesheetId,
        Guid personId,
        Guid docId,
        Guid missionId,
        DateOnly from,
        DateOnly? to,
        DocumentStatus status)
        => new()
        {
            Id = id,
            TimesheetId = timesheetId,
            PersonId = personId,
            CombatTaskDocumentId = docId,
            MissionId = missionId,
            FromDate = from,
            ToDate = to,
            Status = status,
            ClosedByCodeId = null,
            ClosedReference = null,
            UpdatedBy = "seed",
            UpdatedAtUtc = NowUtc
        };

    //======================================================================
    // GetEpisodeOnDateAsync / GetActiveEpisodeAsync / LoadEpisodeOnDateForUpdateAsync
    //======================================================================

    [Fact]
    public async Task GetEpisodeOnDateAsync_returns_null_when_none()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        var res = await repo.GetEpisodeOnDateAsync(Guid.NewGuid(), new DateOnly(2026, 2, 10));
        Assert.Null(res);
    }

    [Fact]
    public async Task GetEpisodeOnDateAsync_returns_episode_covering_date_inclusive()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var epId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimeSheets.Add(NewEpisode(epId, personId, openedAt: new DateOnly(2026, 2, 1), closedAt: new DateOnly(2026, 2, 10)));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        var on5 = await repo.GetEpisodeOnDateAsync(personId, new DateOnly(2026, 2, 5));
        Assert.NotNull(on5);
        Assert.Equal(epId, on5!.Id);

        var on10 = await repo.GetEpisodeOnDateAsync(personId, new DateOnly(2026, 2, 10));
        Assert.NotNull(on10);
        Assert.Equal(epId, on10!.Id);
    }

    [Fact]
    public async Task GetActiveEpisodeAsync_returns_null_when_no_active()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimeSheets.Add(NewEpisode(Guid.NewGuid(), personId, new DateOnly(2026, 2, 1), closedAt: new DateOnly(2026, 2, 5)));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEpisodeRepository(tdb.Factory);
        var active = await repo.GetActiveEpisodeAsync(personId);

        Assert.Null(active);
    }

    [Fact]
    public async Task GetActiveEpisodeAsync_returns_active_episode()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var epId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimeSheets.Add(NewEpisode(epId, personId, new DateOnly(2026, 2, 1), closedAt: null));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEpisodeRepository(tdb.Factory);
        var active = await repo.GetActiveEpisodeAsync(personId);

        Assert.NotNull(active);
        Assert.Equal(epId, active!.Id);
        Assert.Null(active.ClosedAt);
    }

    [Fact]
    public async Task LoadEpisodeOnDateForUpdateAsync_includes_taskspans()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var epId = Guid.NewGuid();

        var spanId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimeSheets.Add(NewEpisode(epId, personId, new DateOnly(2026, 2, 1), closedAt: null));
            db.TimesheetTaskSpans.Add(NewSpan(spanId, epId, personId, docId, missionId, new DateOnly(2026, 2, 5), null, DocumentStatus.Draft));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        var tl = await repo.LoadEpisodeOnDateForUpdateAsync(personId, new DateOnly(2026, 2, 10));

        Assert.NotNull(tl);
        Assert.Single(tl!.TaskSpans);
        Assert.Equal(spanId, tl.TaskSpans[0].Id);
        Assert.Equal(docId, tl.TaskSpans[0].CombatTaskDocumentId);
    }

    //======================================================================
    // OpenOnEnrollAsync
    //======================================================================

    [Fact]
    public async Task OpenOnEnrollAsync_throws_when_author_blank()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.OpenOnEnrollAsync(Guid.NewGuid(), new DateOnly(2026, 2, 10), author: "   ", nowUtc: NowUtc));
    }

    [Fact]
    public async Task OpenOnEnrollAsync_throws_when_personId_empty()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.OpenOnEnrollAsync(Guid.Empty, new DateOnly(2026, 2, 10), author: "u", nowUtc: NowUtc));
    }

    [Fact]
    public async Task OpenOnEnrollAsync_throws_when_default_code_missing()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.OpenOnEnrollAsync(Guid.NewGuid(), new DateOnly(2026, 2, 10), author: "u", nowUtc: NowUtc));

        Assert.Contains("Код", ex.Message);
        Assert.Contains("не знайдено", ex.Message);
    }

    [Fact]
    public async Task OpenOnEnrollAsync_creates_new_episode_and_default_entry_idempotently()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 2, 10);

        // DefaultEnrollCode = TimesheetSystemCodes.BaseState
        var defaultCodeId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(defaultCodeId, TimesheetSystemCodes.BaseState, isActive: true));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        await repo.OpenOnEnrollAsync(personId, enrollDate, author: "  user1 ", nowUtc: NowUtc);
        await repo.OpenOnEnrollAsync(personId, enrollDate, author: "user1", nowUtc: NowUtc); // idempotent

        await using var db2 = await tdb.Factory.CreateDbContextAsync();

        var tl = await db2.TimeSheets.AsNoTracking().SingleAsync(x => x.PersonId == personId);
        Assert.Equal(enrollDate, tl.OpenedAt);
        Assert.Null(tl.ClosedAt);
        Assert.Equal("user1", tl.CreatedBy);

        var entries = await db2.TimesheetEntries.AsNoTracking()
            .Where(x => x.TimesheetId == tl.Id && !x.IsDeleted)
            .ToListAsync();

        var e = Assert.Single(entries);
        Assert.Equal(enrollDate, e.From);
        Assert.Null(e.To);
        Assert.Equal(defaultCodeId, e.TimesheetCodeDefinitionId);
        Assert.Equal("Auto: enroll", e.Reference);
        Assert.Equal("user1", e.CreatedBy);
    }

    [Fact]
    public async Task OpenOnEnrollAsync_throws_when_active_episode_opened_later_than_enrollDate()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var defaultCodeId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(defaultCodeId, TimesheetSystemCodes.BaseState, isActive: true));

            db.TimeSheets.Add(NewEpisode(Guid.NewGuid(), personId, openedAt: new DateOnly(2026, 2, 10), closedAt: null));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.OpenOnEnrollAsync(personId, new DateOnly(2026, 2, 5), author: "u", nowUtc: NowUtc));

        Assert.Contains("активний епізод відкритий пізніше", ex.Message);
    }

    [Fact]
    public async Task OpenOnEnrollAsync_throws_when_enrollDate_is_not_after_lastClosed()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var defaultCodeId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(defaultCodeId, TimesheetSystemCodes.BaseState, isActive: true));

            // only closed episode exists, no active
            db.TimeSheets.Add(NewEpisode(Guid.NewGuid(), personId, openedAt: new DateOnly(2026, 2, 1), closedAt: new DateOnly(2026, 2, 10)));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.OpenOnEnrollAsync(personId, new DateOnly(2026, 2, 10), author: "u", nowUtc: NowUtc));

        Assert.Contains("Табелі не можна накладати", ex.Message);
    }

    [Fact]
    public async Task OpenOnEnrollAsync_allows_open_after_lastClosed()
    {
        // NOTE: цей тест потребує можливості мати кілька епізодів для PersonId.
        // Якщо у вас в test-db UNIQUE(timesheet_aggregates.person_id) — виправте індекс на partial unique:
        // UNIQUE(person_id) WHERE closed_at IS NULL.
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var defaultCodeId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(defaultCodeId, TimesheetSystemCodes.BaseState, isActive: true));
            db.TimeSheets.Add(NewEpisode(Guid.NewGuid(), personId, openedAt: new DateOnly(2026, 2, 1), closedAt: new DateOnly(2026, 2, 10)));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        await repo.OpenOnEnrollAsync(personId, new DateOnly(2026, 2, 11), author: "u", nowUtc: NowUtc);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var episodes = await db2.TimeSheets.AsNoTracking().Where(x => x.PersonId == personId).ToListAsync();

        Assert.Equal(2, episodes.Count);
        Assert.Contains(episodes, x => x.ClosedAt == new DateOnly(2026, 2, 10));
        Assert.Contains(episodes, x => x.ClosedAt == null && x.OpenedAt == new DateOnly(2026, 2, 11));
    }

    //======================================================================
    // ValidateCanCloseOnExcludeAsync
    //======================================================================

    [Fact]
    public async Task ValidateCanCloseOnExcludeAsync_throws_when_no_active_episode()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.ValidateCanCloseOnExcludeAsync(Guid.NewGuid(), new DateOnly(2026, 2, 10)));

        Assert.Equal("Неможливо виключити з табелю: немає активного епізоду.", ex.Message);
    }

    [Fact]
    public async Task ValidateCanCloseOnExcludeAsync_throws_when_no_active_entry_on_date()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimeSheets.Add(NewEpisode(Guid.NewGuid(), personId, openedAt: new DateOnly(2026, 2, 1), closedAt: null));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.ValidateCanCloseOnExcludeAsync(personId, new DateOnly(2026, 2, 10)));

        Assert.Contains("немає активного запису", ex.Message);
    }

    [Fact]
    public async Task ValidateCanCloseOnExcludeAsync_throws_when_code_not_allowed()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var epId = Guid.NewGuid();

        var badCodeId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(badCodeId, "XX", isActive: true));
            db.TimeSheets.Add(NewEpisode(epId, personId, openedAt: new DateOnly(2026, 2, 1), closedAt: null));

            db.TimesheetEntries.Add(NewEntry(Guid.NewGuid(), epId, personId, badCodeId, from: new DateOnly(2026, 2, 1), to: null));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.ValidateCanCloseOnExcludeAsync(personId, new DateOnly(2026, 2, 10)));

        Assert.Contains("Неможливо виключити з табелю зі стану 'XX'", ex.Message);
    }

    [Fact]
    public async Task ValidateCanCloseOnExcludeAsync_allows_T_or_ROZPOR()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var epId = Guid.NewGuid();

        var codeId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            // дозволений код: TimesheetSystemCodes.BaseState (Т)
            db.TimesheetCodes.Add(NewCode(codeId, TimesheetSystemCodes.BaseState, isActive: true));
            db.TimeSheets.Add(NewEpisode(epId, personId, openedAt: new DateOnly(2026, 2, 1), closedAt: null));
            db.TimesheetEntries.Add(NewEntry(Guid.NewGuid(), epId, personId, codeId, from: new DateOnly(2026, 2, 1), to: null));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        await repo.ValidateCanCloseOnExcludeAsync(personId, new DateOnly(2026, 2, 10));
    }

    //======================================================================
    // CloseOnExcludeAsync
    //======================================================================

    [Fact]
    public async Task CloseOnExcludeAsync_throws_when_author_blank()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.CloseOnExcludeAsync(Guid.NewGuid(), new DateOnly(2026, 2, 10), reason: null, author: " ", nowUtc: NowUtc));
    }

    [Fact]
    public async Task CloseOnExcludeAsync_throws_when_personId_empty()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.CloseOnExcludeAsync(Guid.Empty, new DateOnly(2026, 2, 10), reason: null, author: "u", nowUtc: NowUtc));
    }

    [Fact]
    public async Task CloseOnExcludeAsync_throws_when_no_active_episode()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CloseOnExcludeAsync(Guid.NewGuid(), new DateOnly(2026, 2, 10), reason: null, author: "u", nowUtc: NowUtc));

        Assert.Equal("Неможливо виключити з табелю: немає активного епізоду.", ex.Message);
    }

    [Fact]
    public async Task CloseOnExcludeAsync_throws_when_closeTo_before_openedAt()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var epId = Guid.NewGuid();
        var codeId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId, TimesheetSystemCodes.BaseState, isActive: true));
            db.TimeSheets.Add(NewEpisode(epId, personId, openedAt: new DateOnly(2026, 2, 10), closedAt: null));

            // активний запис на дату closeTo потрібен для валідатора
            db.TimesheetEntries.Add(NewEntry(Guid.NewGuid(), epId, personId, codeId, from: new DateOnly(2026, 2, 10), to: null));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CloseOnExcludeAsync(personId, closeTo: new DateOnly(2026, 2, 9), reason: null, author: "u", nowUtc: NowUtc));

        Assert.Contains("він відкритий з 2026-02-10", ex.Message);
    }

    [Fact]
    public async Task CloseOnExcludeAsync_closes_episode_clamps_entries_and_soft_deletes_future_entries_with_reason()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var epId = Guid.NewGuid();

        var codeId = Guid.NewGuid(); // allowed code

        var eOpen = Guid.NewGuid();
        var eLong = Guid.NewGuid();
        var eFuture = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId, TimesheetSystemCodes.BaseState, isActive: true));

            db.TimeSheets.Add(NewEpisode(epId, personId, openedAt: new DateOnly(2026, 2, 1), closedAt: null));

            // Active allowed code covering closeTo (open-ended)
            db.TimesheetEntries.Add(NewEntry(eOpen, epId, personId, codeId, from: new DateOnly(2026, 2, 1), to: null));

            // Another entry that extends beyond closeTo -> should be clamped
            db.TimesheetEntries.Add(NewEntry(eLong, epId, personId, codeId, from: new DateOnly(2026, 2, 5), to: new DateOnly(2026, 2, 20)));

            // Future entry From > closeTo -> should be soft-deleted
            db.TimesheetEntries.Add(NewEntry(eFuture, epId, personId, codeId, from: new DateOnly(2026, 2, 11), to: null));

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        var closeTo = new DateOnly(2026, 2, 10);
        await repo.CloseOnExcludeAsync(personId, closeTo, reason: "  medical  ", author: "  user1  ", nowUtc: NowUtc);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();

        var tl = await db2.TimeSheets.AsNoTracking().SingleAsync(x => x.Id == epId);
        Assert.Equal(closeTo, tl.ClosedAt);
        Assert.Equal("user1", tl.ClosedBy);
        Assert.Equal(NowUtc, tl.ClosedAtUtc);

        var open = await db2.TimesheetEntries.AsNoTracking().SingleAsync(x => x.Id == eOpen);
        Assert.Equal(closeTo, open.To);
        Assert.Equal("user1", open.UpdatedBy);
        Assert.Equal(NowUtc, open.UpdatedAtUtc);

        var lng = await db2.TimesheetEntries.AsNoTracking().SingleAsync(x => x.Id == eLong);
        Assert.Equal(closeTo, lng.To);
        Assert.Equal("user1", lng.UpdatedBy);
        Assert.Equal(NowUtc, lng.UpdatedAtUtc);

        var future = await db2.TimesheetEntries.AsNoTracking().SingleAsync(x => x.Id == eFuture);
        Assert.True(future.IsDeleted);
        Assert.Equal("user1", future.DeletedBy);
        Assert.Equal(NowUtc, future.DeletedAtUtc);
        Assert.Equal("Auto-deleted: person excluded (medical)", future.DeleteReason);
    }

    [Fact]
    public async Task CloseOnExcludeAsync_soft_delete_reason_without_custom_reason()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var epId = Guid.NewGuid();
        var codeId = Guid.NewGuid();

        var eFuture = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId, TimesheetSystemCodes.BaseState, isActive: true));
            db.TimeSheets.Add(NewEpisode(epId, personId, openedAt: new DateOnly(2026, 2, 1), closedAt: null));

            // active allowed entry on close date
            db.TimesheetEntries.Add(NewEntry(Guid.NewGuid(), epId, personId, codeId, from: new DateOnly(2026, 2, 1), to: null));

            // future entry to delete
            db.TimesheetEntries.Add(NewEntry(eFuture, epId, personId, codeId, from: new DateOnly(2026, 2, 20), to: null));

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetEpisodeRepository(tdb.Factory);

        await repo.CloseOnExcludeAsync(personId, new DateOnly(2026, 2, 10), reason: null, author: "u", nowUtc: NowUtc);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var future = await db2.TimesheetEntries.AsNoTracking().SingleAsync(x => x.Id == eFuture);

        Assert.True(future.IsDeleted);
        Assert.Equal("Auto-deleted: person excluded", future.DeleteReason);
    }
}
