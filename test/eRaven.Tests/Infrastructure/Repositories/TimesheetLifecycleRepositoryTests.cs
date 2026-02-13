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

        // seed codes required by lifecycle repo
        var cT = NewCode("Т");

        await using (var dbSeed = await tdb.Factory.CreateDbContextAsync())
        {
            dbSeed.TimesheetCodes.Add(cT);
            await dbSeed.SaveChangesAsync();
        }

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
            .Include(x => x.TimesheetCodeDefinition)
            .Where(x => x.PersonId == personId && !x.IsDeleted)
            .ToListAsync();

        Assert.Single(entries);

        var e = entries[0];
        Assert.Equal(tl.Id, e.TimelineId);
        Assert.Equal(cT.Id, e.TimesheetCodeDefinitionId);
        Assert.Equal("Т", e.TimesheetCodeDefinition!.Code);
        Assert.Equal(enrollDate, e.From);
        Assert.Null(e.To);
        Assert.Equal("Auto: enroll", e.Reference);
    }

    [Fact]
    public async Task OpenOnEnrollAsync_is_idempotent()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetLifecycleRepository(tdb.Factory);

        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 1, 10);

        // seed default "Т"
        var cT = NewCode("Т");
        await using (var dbSeed = await tdb.Factory.CreateDbContextAsync())
        {
            dbSeed.TimesheetCodes.Add(cT);
            await dbSeed.SaveChangesAsync();
        }

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

        // seed codes used in validation text + actual current code
        var cT = NewCode("Т");
        var cRozpor = NewCode("РОЗПОР");
        var cVp = NewCode("ВП"); // not allowed

        await using (var db = await tdb.Factory.CreateDbContextAsync())
        {
            db.TimesheetCodes.AddRange(cT, cRozpor, cVp);

            var tl = NewTimeline(personId, openedAt, nowUtc);
            db.TimesheetTimelines.Add(tl);

            db.TimesheetEntries.Add(NewEntry(
                timelineId: tl.Id,
                personId: personId,
                codeId: cVp.Id,
                from: openedAt,
                to: null,
                nowUtc: nowUtc));

            await db.SaveChangesAsync();
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await repo.ValidateCanCloseOnExcludeAsync(personId, closeTo: new DateOnly(2026, 1, 5)));

        Assert.Contains("ВП", ex.Message);
        Assert.Contains("Т", ex.Message);
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

        // seed codes needed
        var c30 = NewCode("30");
        var cRozpor = NewCode("РОЗПОР");
        var cPlan = NewCode("ПЛАН");
        var cT = NewCode("Т"); // repo may reference allowed close codes list

        await using (var db = await tdb.Factory.CreateDbContextAsync())
        {
            db.TimesheetCodes.AddRange(c30, cRozpor, cPlan, cT);

            var tl = NewTimeline(personId, openedAt, nowUtc);
            tlId = tl.Id;

            db.TimesheetTimelines.Add(tl);

            // 30 [1..4], РОЗПОР [5..∞] (allowed to close on 15th)
            db.TimesheetEntries.Add(NewEntry(tl.Id, personId, c30.Id, openedAt, new DateOnly(2026, 1, 4), nowUtc));
            db.TimesheetEntries.Add(NewEntry(tl.Id, personId, cRozpor.Id, new DateOnly(2026, 1, 5), null, nowUtc));

            // future plan must be deleted
            db.TimesheetEntries.Add(NewEntry(
                tl.Id,
                personId,
                cPlan.Id,
                new DateOnly(2026, 1, 16),
                new DateOnly(2026, 1, 20),
                nowUtc));

            await db.SaveChangesAsync();
        }

        await repo.CloseOnExcludeAsync(personId, closeTo, reason: "test", author: "ui", nowUtc: DateTime.UtcNow);

        await using (var db = await tdb.Factory.CreateDbContextAsync())
        {
            var tl = await db.TimesheetTimelines.AsNoTracking().SingleAsync(x => x.Id == tlId);
            Assert.Equal(closeTo, tl.ClosedAt);

            var rozpor = await db.TimesheetEntries.AsNoTracking()
                .Include(x => x.TimesheetCodeDefinition)
                .SingleAsync(x => x.TimelineId == tlId && x.TimesheetCodeDefinition!.Code == "РОЗПОР");

            Assert.Equal(closeTo, rozpor.To);

            var futurePlan = await db.TimesheetEntries.AsNoTracking()
                .Include(x => x.TimesheetCodeDefinition)
                .SingleAsync(x => x.TimelineId == tlId && x.TimesheetCodeDefinition!.Code == "ПЛАН");

            Assert.True(futurePlan.IsDeleted);
            Assert.Contains("excluded", futurePlan.DeleteReason ?? "");
        }
    }

    // ----------------------------------------------------------------------
    // helpers
    // ----------------------------------------------------------------------

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
            CreatedAtUtc = DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Utc)
        };

    private static TimesheetEntry NewEntry(
        Guid timelineId,
        Guid personId,
        Guid codeId,
        DateOnly from,
        DateOnly? to,
        DateTime nowUtc)
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
            CreatedBy = "seed",
            CreatedAtUtc = nowUtc,
            IsDeleted = false
        };
}
