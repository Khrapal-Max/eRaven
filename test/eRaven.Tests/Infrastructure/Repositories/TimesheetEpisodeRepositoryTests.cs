//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEpisodeRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetEpisodeRepositoryTests
{
    [Fact]
    public async Task OpenOnEnrollAsync_CreatesEpisodeAndDefaultEntry()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var repo = new TimesheetEpisodeRepository(db.Factory);

        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 02, 10);

        await repo.OpenOnEnrollAsync(personId, enrollDate, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        await using var ctx = db.Factory.CreateDbContext();
        var ep = await ctx.TimeSheets
            .Include(x => x.Entries
                .Where(x => !x.IsDeleted))
            .SingleAsync(x => x.PersonId == personId);

        Assert.Equal(enrollDate, ep.OpenedAt);
        Assert.Null(ep.ClosedAt);
        Assert.Single(ep.Entries);

        var entry = ep.Entries.Single(x => !x.IsDeleted);
        Assert.Equal(enrollDate, entry.From);
        Assert.Null(entry.To);
        Assert.Equal("Auto: enroll", entry.Reference);
        Assert.Equal(TimesheetRepoTestHelpers.Author, entry.CreatedBy);
        Assert.Equal(TimesheetRepoTestHelpers.NowUtc, entry.CreatedAtUtc);
    }

    [Fact]
    public async Task OpenOnEnrollAsync_IsIdempotent_DoesNotDuplicateEntry()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var repo = new TimesheetEpisodeRepository(db.Factory);

        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 02, 10);

        await repo.OpenOnEnrollAsync(personId, enrollDate, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);
        await repo.OpenOnEnrollAsync(personId, enrollDate, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        await using var ctx = db.Factory.CreateDbContext();
        var eps = await ctx.TimeSheets
            .Include(x => x.Entries
                .Where(x => !x.IsDeleted))
            .Where(x => x.PersonId == personId)
            .ToListAsync();

        Assert.Single(eps);
    }

    [Fact]
    public async Task OpenOnEnrollAsync_RejectsBackdatedEnroll_WhenClosedEpisodeExists()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var personId = Guid.NewGuid();

        await using (var ctx = db.Factory.CreateDbContext())
        {
            ctx.TimeSheets.Add(new TimeSheetAggregate
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                OpenedAt = new DateOnly(2026, 01, 01),
                ClosedAt = new DateOnly(2026, 02, 10),
                CreatedBy = TimesheetRepoTestHelpers.Author,
                CreatedAtUtc = TimesheetRepoTestHelpers.NowUtc
            });

            await ctx.SaveChangesAsync();
        }

        var repo = new TimesheetEpisodeRepository(db.Factory);
        var enrollDate = new DateOnly(2026, 02, 10);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await repo.OpenOnEnrollAsync(personId, enrollDate, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc));
    }

    [Fact]
    public async Task ValidateCanCloseOnExcludeAsync_Throws_WhenCodeNotAllowed()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var epRepo = new TimesheetEpisodeRepository(db.Factory);
        var writer = new TimesheetEntryWriterRepository(db.Factory);

        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 02, 10);
        await epRepo.OpenOnEnrollAsync(personId, enrollDate, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var vdrId = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "ВДР");
        var closeTo = new DateOnly(2026, 02, 12);

        // Put person into an unsupported close state on closeTo.
        await writer.TransitionAsync(personId, closeTo, vdrId, reference: "test", note: null, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await epRepo.ValidateCanCloseOnExcludeAsync(personId, closeTo));
    }

    [Fact]
    public async Task CloseOnExcludeAsync_ClosesEpisode_AndSoftDeletesFutureEntries()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var epRepo = new TimesheetEpisodeRepository(db.Factory);
        var writer = new TimesheetEntryWriterRepository(db.Factory);

        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 02, 10);
        await epRepo.OpenOnEnrollAsync(personId, enrollDate, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var code30 = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "30");

        // Add a future entry that must be auto-deleted on close.
        var futureDate = new DateOnly(2026, 02, 15);
        var futureId = await writer.AddEntryAsync(personId, futureDate, code30, reference: "future", note: null,
            TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var closeTo = new DateOnly(2026, 02, 12);
        await epRepo.CloseOnExcludeAsync(personId, closeTo, reason: "test", TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        await using var ctx = db.Factory.CreateDbContext();
        var ep = await ctx.TimeSheets.Include(x => x.Entries).SingleAsync(x => x.PersonId == personId);

        Assert.Equal(closeTo, ep.ClosedAt);

        // Last active entry must be clamped to ClosedAt+1.
        var closeExclusive = closeTo.AddDays(1);
        var lastActive = ep.Entries
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.From)
            .First();

        Assert.Equal(closeExclusive, lastActive.To);
        Assert.Equal(TimesheetRepoTestHelpers.Author, lastActive.UpdatedBy);
        Assert.Equal(TimesheetRepoTestHelpers.NowUtc, lastActive.UpdatedAtUtc);

        // Future entry must be soft-deleted.
        var deleted = ep.Entries.Single(x => x.Id == futureId);
        Assert.True(deleted.IsDeleted);
        Assert.Equal(TimesheetRepoTestHelpers.Author, deleted.DeletedBy);
        Assert.Equal(TimesheetRepoTestHelpers.NowUtc, deleted.DeletedAtUtc);
        Assert.Contains("Auto-deleted: person excluded", deleted.DeleteReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
