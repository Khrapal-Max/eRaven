//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetAggregateRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetAggregateRepositoryTests
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
            CreatedAtUtc = NowUtc.AddHours(-1),
            ClosedBy = null,
            ClosedAtUtc = null
        };

    private static TimesheetEntry NewEntry(
        Guid id,
        Guid personId,
        Guid codeId,
        DateOnly from,
        DateOnly? to = null)
        => new()
        {
            Id = id,
            TimesheetId = Guid.Empty,   // set by relationship fix-up (principal.Entries.Add)
            TimeSheet = null,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeId,
            TimesheetCodeDefinition = null,
            From = from,
            To = to,
            Reference = null,
            Note = null,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc,
            UpdatedBy = null,
            UpdatedAtUtc = null,
            IsDeleted = false
        };

    private static TimesheetTaskSpan NewSpan(
        Guid id,
        Guid personId,
        Guid documentId,
        Guid missionId,
        DateOnly from,
        DateOnly? to,
        DocumentStatus status)
        => new()
        {
            Id = id,
            TimesheetId = Guid.Empty, // set by relationship fix-up (principal.TaskSpans.Add)
            PersonId = personId,
            CombatTaskDocumentId = documentId,
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
    // LoadActiveForUpdateAsync
    //======================================================================

    [Fact]
    public async Task LoadActiveForUpdateAsync_throws_on_empty_person_id()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetAggregateRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.LoadActiveForUpdateAsync(Guid.Empty));
    }

    [Fact]
    public async Task LoadActiveForUpdateAsync_returns_null_when_no_active_episode()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var codeId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId));
            db.TimeSheets.Add(NewEpisode(
                id: Guid.NewGuid(),
                personId: personId,
                openedAt: new DateOnly(2026, 2, 1),
                closedAt: new DateOnly(2026, 2, 10))); // closed
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetAggregateRepository(tdb.Factory);

        var ep = await repo.LoadActiveForUpdateAsync(personId);

        Assert.Null(ep);
    }

    [Fact]
    public async Task LoadActiveForUpdateAsync_returns_tracked_episode_with_entries_and_taskspans_loaded()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var codeId = Guid.NewGuid();

        var episodeId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var spanId = Guid.NewGuid();

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode(codeId));

            var ep = NewEpisode(
                id: episodeId,
                personId: personId,
                openedAt: new DateOnly(2026, 2, 1),
                closedAt: null);

            ep.Entries.Add(NewEntry(entryId, personId, codeId, from: new DateOnly(2026, 2, 1)));
            ep.TaskSpans.Add(NewSpan(spanId, personId, docId, missionId, from: new DateOnly(2026, 2, 5), to: null, status: DocumentStatus.Draft));

            db.TimeSheets.Add(ep);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetAggregateRepository(tdb.Factory);

        var loaded = await repo.LoadActiveForUpdateAsync(personId);

        Assert.NotNull(loaded);
        Assert.Equal(episodeId, loaded!.Id);
        Assert.Equal(personId, loaded.PersonId);
        Assert.Null(loaded.ClosedAt);

        // INCLUDE must load dependents (not 0)
        Assert.Single(loaded.Entries);
        Assert.Single(loaded.TaskSpans);

        Assert.Equal(entryId, loaded.Entries[0].Id);
        Assert.Equal(spanId, loaded.TaskSpans[0].Id);
        Assert.Equal(docId, loaded.TaskSpans[0].CombatTaskDocumentId);
    }

    //======================================================================
    // LoadOnDateForUpdateAsync
    //======================================================================

    [Fact]
    public async Task LoadOnDateForUpdateAsync_throws_on_empty_person_id()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetAggregateRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.LoadOnDateForUpdateAsync(Guid.Empty, new DateOnly(2026, 2, 10)));
    }

    [Fact]
    public async Task LoadOnDateForUpdateAsync_returns_null_when_no_episode_covers_date()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimeSheets.Add(NewEpisode(
                id: Guid.NewGuid(),
                personId: personId,
                openedAt: new DateOnly(2026, 2, 1),
                closedAt: new DateOnly(2026, 2, 5)));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetAggregateRepository(tdb.Factory);

        var loaded = await repo.LoadOnDateForUpdateAsync(personId, new DateOnly(2026, 2, 10));

        Assert.Null(loaded);
    }

    [Fact]
    public async Task LoadOnDateForUpdateAsync_returns_episode_covering_date()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var epId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimeSheets.Add(NewEpisode(
                id: epId,
                personId: personId,
                openedAt: new DateOnly(2026, 2, 1),
                closedAt: new DateOnly(2026, 2, 20)));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetAggregateRepository(tdb.Factory);

        var loaded = await repo.LoadOnDateForUpdateAsync(personId, new DateOnly(2026, 2, 10));

        Assert.NotNull(loaded);
        Assert.Equal(epId, loaded!.Id);
    }

    [Fact]
    public async Task LoadOnDateForUpdateAsync_when_multiple_episodes_cover_date_picks_latest_by_openedAt_desc_then_id_desc()
    {
        // Це тест на "стійкість" до поганих даних (перетини епізодів).
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();

        // Same OpenedAt => tie-break by Id desc
        var openedAt = new DateOnly(2026, 2, 1);
        var coverTo = new DateOnly(2026, 2, 20);

        var idSmall = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var idBig = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimeSheets.Add(NewEpisode(idSmall, personId, openedAt, coverTo));
            db.TimeSheets.Add(NewEpisode(idBig, personId, openedAt, coverTo));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetAggregateRepository(tdb.Factory);

        var loaded = await repo.LoadOnDateForUpdateAsync(personId, new DateOnly(2026, 2, 10));

        Assert.NotNull(loaded);
        Assert.Equal(idBig, loaded!.Id);
    }

    //======================================================================
    // SaveAsync
    //======================================================================

    [Fact]
    public async Task SaveAsync_when_no_context_loaded_should_noop()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetAggregateRepository(tdb.Factory);

        await repo.SaveAsync(); // must not throw
    }

    [Fact]
    public async Task SaveAsync_persists_changes_for_tracked_episode()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimeSheets.Add(NewEpisode(
                id: episodeId,
                personId: personId,
                openedAt: new DateOnly(2026, 2, 1),
                closedAt: null));
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetAggregateRepository(tdb.Factory);

        var loaded = await repo.LoadActiveForUpdateAsync(personId);
        Assert.NotNull(loaded);

        // Modify tracked entity
        loaded!.ClosedAt = new DateOnly(2026, 2, 5);
        loaded.ClosedBy = "tester";
        loaded.ClosedAtUtc = NowUtc;

        await repo.SaveAsync();

        // Verify persisted in a new context
        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.TimeSheets.AsNoTracking().SingleAsync(x => x.Id == episodeId);

        Assert.Equal(new DateOnly(2026, 2, 5), reloaded.ClosedAt);
        Assert.Equal("tester", reloaded.ClosedBy);
        Assert.Equal(NowUtc, reloaded.ClosedAtUtc);
    }
}
