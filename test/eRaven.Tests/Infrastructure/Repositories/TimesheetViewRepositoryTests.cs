//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetViewRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Consts;
using eRaven.Domain.Enums;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetViewRepositoryTests
{
    private const string Author = "tests";

    [Fact]
    public async Task GetTimesheetsMonthAsync_EnrollMidMonth_ReturnsFullMonthMatrixWithDerivedPrefix()
    {
        await using var testDb = new SqliteTestDb();
        await SeedPolicyAsync(testDb);

        var nowUtc = new DateTime(2026, 02, 01, 12, 0, 0, DateTimeKind.Utc);

        // Person A enrolls mid-month (10.02)
        var personA = Guid.NewGuid();
        var episodeAId = Guid.NewGuid();

        // Person B enrolls at month start (01.02)
        var personB = Guid.NewGuid();
        var episodeBId = Guid.NewGuid();

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var tCodeId = await GetCodeIdAsync(db, "Т");

            db.TimeSheets.Add(CreateEpisodeWithSingleEntry(
                id: episodeAId,
                personId: personA,
                openedAt: new DateOnly(2026, 02, 10),
                codeId: tCodeId,
                author: Author,
                nowUtc: nowUtc,
                reference: "ENROLL-A"));

            db.TimeSheets.Add(CreateEpisodeWithSingleEntry(
                id: episodeBId,
                personId: personB,
                openedAt: new DateOnly(2026, 02, 01),
                codeId: tCodeId,
                author: Author,
                nowUtc: nowUtc,
                reference: "ENROLL-B"));

            await db.SaveChangesAsync();
        }

        var sut = new TimesheetViewRepository(testDb.Factory);
        var result = await sut.GetTimesheetsMonthAsync(2026, 2);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var periodA = result.Single(x => x.PersonId == personA);
        var periodB = result.Single(x => x.PersonId == personB);

        Assert.NotEqual(Guid.Empty, personA);
        Assert.NotEqual(Guid.Empty, personB);

        // February 2026 has 28 days
        Assert.Equal(28, periodA.Days.Count);
        Assert.Equal(28, periodB.Days.Count);

        // Person A: 01..09 = derived NB, 10..28 = "Т"
        for (var day = 1; day <= 9; day++)
        {
            var rm = periodA.Days[day - 1];
            Assert.Equal(new DateOnly(2026, 02, day), rm.DateOfDay);
            Assert.True(rm.IsDerived);
            Assert.False(rm.IsChangePoint);
            Assert.Null(rm.CodeId);
            Assert.Equal(Guid.Empty, rm.TimesheetId);
            Assert.Equal(TimesheetDerivedCodes.NotInTimesheet, rm.Code);
            Assert.Equal(TimesheetUiStyle.NotInTimesheet, rm.UiStyle);
        }

        for (var day = 10; day <= 28; day++)
        {
            var rm = periodA.Days[day - 1];
            Assert.Equal(new DateOnly(2026, 02, day), rm.DateOfDay);
            Assert.False(rm.IsDerived);
            Assert.Equal("Т", rm.Code);
            Assert.Equal(episodeAId, rm.TimesheetId);

            if (day == 10)
                Assert.True(rm.IsChangePoint);
            else
                Assert.False(rm.IsChangePoint);
        }

        // Person B: all days are "Т", change-point only on 01.02
        for (var day = 1; day <= 28; day++)
        {
            var rm = periodB.Days[day - 1];
            Assert.Equal(new DateOnly(2026, 02, day), rm.DateOfDay);
            Assert.False(rm.IsDerived);
            Assert.Equal("Т", rm.Code);
            Assert.Equal(episodeBId, rm.TimesheetId);

            if (day == 1)
                Assert.True(rm.IsChangePoint);
            else
                Assert.False(rm.IsChangePoint);
        }
    }

    [Fact]
    public async Task GetTimesheetsMonthAsync_ClosedMidMonth_ReturnsDerivedSuffixAfterClose()
    {
        await using var testDb = new SqliteTestDb();
        await SeedPolicyAsync(testDb);

        var nowUtc = new DateTime(2026, 02, 01, 12, 0, 0, DateTimeKind.Utc);

        var personId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var tCodeId = await GetCodeIdAsync(db, "Т");

            var episode = CreateEpisodeWithSingleEntry(
                id: episodeId,
                personId: personId,
                openedAt: new DateOnly(2026, 02, 01),
                codeId: tCodeId,
                author: Author,
                nowUtc: nowUtc,
                reference: "ENROLL");

            episode.ClosedAt = new DateOnly(2026, 02, 15);
            episode.ClosedBy = Author;
            episode.ClosedAtUtc = nowUtc.AddDays(15);

            db.TimeSheets.Add(episode);
            await db.SaveChangesAsync();
        }

        var sut = new TimesheetViewRepository(testDb.Factory);
        var result = await sut.GetTimesheetsMonthAsync(2026, 2);

        var period = Assert.Single(result);
        Assert.Equal(28, period.Days.Count);

        // 01..15 = "Т"
        for (var day = 1; day <= 15; day++)
        {
            var rm = period.Days[day - 1];
            Assert.False(rm.IsDerived);
            Assert.Equal("Т", rm.Code);
        }

        // 16..28 = derived NB (episode ended at 15, endExclusive = 16)
        for (var day = 16; day <= 28; day++)
        {
            var rm = period.Days[day - 1];
            Assert.True(rm.IsDerived);
            Assert.Equal(TimesheetDerivedCodes.NotInTimesheet, rm.Code);
            Assert.Equal(TimesheetUiStyle.NotInTimesheet, rm.UiStyle);
        }
    }

    [Fact]
    public async Task GetTimesheetPersonMonthAsync_ReturnsOnlyRequestedPersonOrNull()
    {
        await using var testDb = new SqliteTestDb();
        await SeedPolicyAsync(testDb);

        var nowUtc = new DateTime(2026, 02, 01, 12, 0, 0, DateTimeKind.Utc);

        var personA = Guid.NewGuid();
        var personB = Guid.NewGuid();

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var tCodeId = await GetCodeIdAsync(db, "Т");

            db.TimeSheets.Add(CreateEpisodeWithSingleEntry(
                id: Guid.NewGuid(),
                personId: personA,
                openedAt: new DateOnly(2026, 02, 10),
                codeId: tCodeId,
                author: Author,
                nowUtc: nowUtc));

            db.TimeSheets.Add(CreateEpisodeWithSingleEntry(
                id: Guid.NewGuid(),
                personId: personB,
                openedAt: new DateOnly(2026, 02, 01),
                codeId: tCodeId,
                author: Author,
                nowUtc: nowUtc));

            await db.SaveChangesAsync();
        }

        var sut = new TimesheetViewRepository(testDb.Factory);

        var periodA = await sut.GetTimesheetPersonMonthAsync(personA, 2026, 2);
        Assert.NotNull(periodA);
        Assert.Equal(personA, periodA!.PersonId);
        Assert.Equal(28, periodA.Days.Count);
        Assert.True(periodA.Days.Take(9).All(x => x.IsDerived));
        Assert.True(periodA.Days.Skip(9).All(x => !x.IsDerived));

        var missing = await sut.GetTimesheetPersonMonthAsync(Guid.NewGuid(), 2026, 2);
        Assert.Null(missing);
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static async Task SeedPolicyAsync(SqliteTestDb testDb)
    {
        await using var db = await testDb.Factory.CreateDbContextAsync();
        await TimesheetPolicySeed.EnsureSeedAsync(db);
    }

    private static async Task<Guid> GetCodeIdAsync(AppDbContext db, string code)
        => await db.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.Code == code)
            .Select(x => x.Id)
            .SingleAsync();

    private static TimeSheetAggregate CreateEpisodeWithSingleEntry(
        Guid id,
        Guid personId,
        DateOnly openedAt,
        Guid codeId,
        string author,
        DateTime nowUtc,
        string? reference = null)
    {
        var episode = new TimeSheetAggregate
        {
            Id = id,
            PersonId = personId,
            OpenedAt = openedAt,
            CreatedBy = author,
            CreatedAtUtc = nowUtc,
            Entries = []
        };

        episode.Entries.Add(new eRaven.Domain.Entities.TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = id,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeId,
            From = openedAt,
            To = null,
            Reference = reference ?? string.Empty,
            CreatedBy = author,
            CreatedAtUtc = nowUtc
        });

        return episode;
    }
}