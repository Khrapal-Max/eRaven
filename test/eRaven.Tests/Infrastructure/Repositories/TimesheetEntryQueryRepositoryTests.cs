//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntryQueryRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetEntryQueryRepositoryTests
{
    [Fact]
    public async Task GetEntriesForPersonAsync_ReturnsOverlapping_Ordered_AndExcludesDeleted()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var epRepo = new TimesheetEpisodeRepository(db.Factory);
        var writer = new TimesheetEntryWriterRepository(db.Factory);
        var query = new TimesheetEntryQueryRepository(db.Factory);

        var personId = Guid.NewGuid();
        var d0 = new DateOnly(2026, 02, 10);
        await epRepo.OpenOnEnrollAsync(personId, d0, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var code30 = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "30");
        var vdr = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "ВДР");

        var d3 = d0.AddDays(3);
        var d10 = d0.AddDays(10);

        await writer.AddEntryAsync(personId, d3, code30, reference: "r3", note: null, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);
        var delId = await writer.AddEntryAsync(personId, d10, vdr, reference: "r10", note: null, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        await writer.SoftDeleteAsync(delId, reason: "del", TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var result = await query.GetEntriesForPersonAsync(personId, from: d0.AddDays(2), toExclusive: d0.AddDays(11));

        Assert.Equal(2, result.Count); // d0 + d3 (d10 deleted)
        Assert.True(result.SequenceEqual(result.OrderBy(x => x.From)));
        Assert.DoesNotContain(result, x => x.Id == delId);
    }

    [Fact]
    public async Task GetEntriesForPersonsAsync_ReturnsSortedByPersonThenFrom()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var epRepo = new TimesheetEpisodeRepository(db.Factory);
        var writer = new TimesheetEntryWriterRepository(db.Factory);
        var query = new TimesheetEntryQueryRepository(db.Factory);

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var d0 = new DateOnly(2026, 02, 10);
        await epRepo.OpenOnEnrollAsync(p1, d0, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);
        await epRepo.OpenOnEnrollAsync(p2, d0, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var code30 = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "30");

        await writer.AddEntryAsync(p1, d0.AddDays(5), code30, reference: null, note: null, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);
        await writer.AddEntryAsync(p2, d0.AddDays(3), code30, reference: null, note: null, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var result = await query.GetEntriesForPersonsAsync([p1, p2], from: d0, toExclusive: d0.AddDays(8));

        var sorted = result
            .OrderBy(x => x.PersonId)
            .ThenBy(x => x.From)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(sorted, [.. result.Select(x => x.Id)]);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenSoftDeleted()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var epRepo = new TimesheetEpisodeRepository(db.Factory);
        var writer = new TimesheetEntryWriterRepository(db.Factory);
        var query = new TimesheetEntryQueryRepository(db.Factory);

        var personId = Guid.NewGuid();
        var d0 = new DateOnly(2026, 02, 10);
        await epRepo.OpenOnEnrollAsync(personId, d0, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var code30 = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "30");
        var id = await writer.AddEntryAsync(personId, d0.AddDays(3), code30, reference: null, note: null,
            TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        await writer.SoftDeleteAsync(id, reason: "x", TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var res = await query.GetByIdAsync(id);
        Assert.Null(res);
    }

    [Fact]
    public async Task GetActiveEntryOnDateAsync_ReturnsEntry_AndLoadsDefinition()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var epRepo = new TimesheetEpisodeRepository(db.Factory);
        var writer = new TimesheetEntryWriterRepository(db.Factory);
        var query = new TimesheetEntryQueryRepository(db.Factory);

        var personId = Guid.NewGuid();
        var d0 = new DateOnly(2026, 02, 10);
        await epRepo.OpenOnEnrollAsync(personId, d0, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var code30 = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "30");
        var d5 = d0.AddDays(5);
        await writer.AddEntryAsync(personId, d5, code30, reference: "r", note: null,
            TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var onDate = d0.AddDays(6);
        var active = await query.GetActiveEntryOnDateAsync(personId, onDate);

        Assert.NotNull(active);
        Assert.Equal(d5, active!.From);
        Assert.Equal(code30, active.TimesheetCodeDefinitionId);
        Assert.NotNull(active.TimesheetCodeDefinition);
        Assert.Equal("30", active.TimesheetCodeDefinition!.Code);
    }

    [Fact]
    public async Task GetNextEntryAfterDateAsync_ReturnsNextAnchor()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var epRepo = new TimesheetEpisodeRepository(db.Factory);
        var writer = new TimesheetEntryWriterRepository(db.Factory);
        var query = new TimesheetEntryQueryRepository(db.Factory);

        var personId = Guid.NewGuid();
        var d0 = new DateOnly(2026, 02, 10);
        await epRepo.OpenOnEnrollAsync(personId, d0, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var code30 = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "30");

        var d3 = d0.AddDays(3);
        var d10 = d0.AddDays(10);
        await writer.AddEntryAsync(personId, d3, code30, reference: null, note: null,
            TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);
        await writer.AddEntryAsync(personId, d10, code30, reference: null, note: null,
            TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var next = await query.GetNextEntryAfterDateAsync(personId, d3);
        Assert.NotNull(next);
        Assert.Equal(d10, next!.From);

        var none = await query.GetNextEntryAfterDateAsync(personId, d10);
        Assert.Null(none);
    }
}
