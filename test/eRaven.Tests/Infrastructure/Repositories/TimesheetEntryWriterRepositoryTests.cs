//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntryWriterRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetEntryWriterRepositoryTests
{
    [Fact]
    public async Task TransitionAsync_WhenAnchorExists_ReplacesInPlace()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var epRepo = new TimesheetEpisodeRepository(db.Factory);
        var writer = new TimesheetEntryWriterRepository(db.Factory);

        var personId = Guid.NewGuid();
        var d0 = new DateOnly(2026, 02, 10);
        await epRepo.OpenOnEnrollAsync(personId, d0, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        await using var ctx0 = db.Factory.CreateDbContext();
        var originalId = await ctx0.TimesheetEntries
            .AsNoTracking()
            .Where(x => x.PersonId == personId && !x.IsDeleted)
            .Where(x => x.From == d0)
            .Select(x => x.Id)
            .SingleAsync();

        var code30 = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "30");

        var returnedId = await writer.TransitionAsync(personId, d0, code30, reference: "r", note: null,
            TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        Assert.Equal(originalId, returnedId);

        await using var ctx = db.Factory.CreateDbContext();
        var entry = await ctx.TimesheetEntries
            .Include(x => x.TimesheetCodeDefinition)
            .SingleAsync(x => x.Id == originalId);

        Assert.Equal(code30, entry.TimesheetCodeDefinitionId);
        Assert.Equal("r", entry.Reference);
        Assert.Equal(TimesheetRepoTestHelpers.Author, entry.UpdatedBy);
        Assert.Equal(TimesheetRepoTestHelpers.NowUtc, entry.UpdatedAtUtc);
    }

    [Fact]
    public async Task AddEntryAsync_AddsNewAnchor_AndNormalizesIntervals()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var epRepo = new TimesheetEpisodeRepository(db.Factory);
        var writer = new TimesheetEntryWriterRepository(db.Factory);

        var personId = Guid.NewGuid();
        var d0 = new DateOnly(2026, 02, 10);
        await epRepo.OpenOnEnrollAsync(personId, d0, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var code30 = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "30");
        var d3 = d0.AddDays(3);

        var createdId = await writer.AddEntryAsync(personId, d3, code30, reference: "ref", note: null,
            TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        Assert.NotEqual(Guid.Empty, createdId);

        await using var ctx = db.Factory.CreateDbContext();
        var _entries = await ctx.TimesheetEntries
            .AsNoTracking()
            .Where(x => x.PersonId == personId && !x.IsDeleted)
            .OrderBy(x => x.From)
            .ToListAsync();

        Assert.Equal(2, _entries.Count);
        Assert.Equal(d0, _entries[0].From);
        Assert.Equal(d3, _entries[0].To);
        Assert.Equal(d3, _entries[1].From);
        Assert.Null(_entries[1].To);
    }

    [Fact]
    public async Task CorrectEntryAsync_MovesAnchorAndRenormalizes()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var epRepo = new TimesheetEpisodeRepository(db.Factory);
        var writer = new TimesheetEntryWriterRepository(db.Factory);

        var personId = Guid.NewGuid();
        var d0 = new DateOnly(2026, 02, 10);
        await epRepo.OpenOnEnrollAsync(personId, d0, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var code30 = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "30");
        var d3 = d0.AddDays(3);
        var id = await writer.AddEntryAsync(personId, d3, code30, reference: null, note: null,
            TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var vdr = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "ВДР");
        var d5 = d0.AddDays(5);

        await writer.CorrectEntryAsync(personId, id, vdr, d5, reference: "r", note: "n",
            TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        await using var ctx = db.Factory.CreateDbContext();
        var active = await ctx.TimesheetEntries
            .AsNoTracking()
            .Where(x => x.PersonId == personId && !x.IsDeleted)
            .OrderBy(x => x.From)
            .ToListAsync();

        Assert.Equal(2, active.Count);
        Assert.Equal(d0, active[0].From);
        Assert.Equal(d5, active[0].To);
        Assert.Equal(d5, active[1].From);
        Assert.Null(active[1].To);
        Assert.Equal(vdr, active[1].TimesheetCodeDefinitionId);
        Assert.Equal("r", active[1].Reference);
        Assert.Equal("n", active[1].Note);
    }

    [Fact]
    public async Task RemoveEntryAsync_RemovesAnchor_AndRenormalizes()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var epRepo = new TimesheetEpisodeRepository(db.Factory);
        var writer = new TimesheetEntryWriterRepository(db.Factory);

        var personId = Guid.NewGuid();
        var d0 = new DateOnly(2026, 02, 10);
        await epRepo.OpenOnEnrollAsync(personId, d0, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var code30 = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "30");
        var d3 = d0.AddDays(3);
        var id = await writer.AddEntryAsync(personId, d3, code30, reference: null, note: null,
            TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        await writer.RemoveEntryAsync(personId, id, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        await using var ctx = db.Factory.CreateDbContext();
        var active = await ctx.TimesheetEntries
            .AsNoTracking()
            .Where(x => x.PersonId == personId && !x.IsDeleted)
            .OrderBy(x => x.From)
            .ToListAsync();

        Assert.Single(active);
        Assert.Equal(d0, active[0].From);
        Assert.Null(active[0].To);
    }

    [Fact]
    public async Task SoftDeleteAsync_SoftDeletesAnchor_AndRenormalizes()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var epRepo = new TimesheetEpisodeRepository(db.Factory);
        var writer = new TimesheetEntryWriterRepository(db.Factory);

        var personId = Guid.NewGuid();
        var d0 = new DateOnly(2026, 02, 10);
        await epRepo.OpenOnEnrollAsync(personId, d0, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var code30 = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "30");
        var d3 = d0.AddDays(3);
        var id = await writer.AddEntryAsync(personId, d3, code30, reference: null, note: null,
            TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        await writer.SoftDeleteAsync(id, reason: "test", TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        await using var ctx = db.Factory.CreateDbContext();
        var _entries = await ctx.TimesheetEntries
            .AsNoTracking()
            .Where(x => x.PersonId == personId)
            .OrderBy(x => x.From)
            .ToListAsync();

        Assert.Equal(2, _entries.Count);
        Assert.False(_entries[0].IsDeleted);
        Assert.True(_entries[1].IsDeleted);
        Assert.Equal("test", _entries[1].DeleteReason);

        // active interval should be open-ended again
        Assert.Null(_entries[0].To);
    }

    [Fact]
    public async Task ApplyChangePointsAsync_NormalizesInput_SortsAndKeepsLastPerDate()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var epRepo = new TimesheetEpisodeRepository(db.Factory);
        var writer = new TimesheetEntryWriterRepository(db.Factory);

        var personId = Guid.NewGuid();
        var d0 = new DateOnly(2026, 02, 10);
        await epRepo.OpenOnEnrollAsync(personId, d0, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var code30 = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "30");
        var vdr = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "ВДР");

        var d2 = d0.AddDays(2);
        var d3 = d0.AddDays(3);

        // unordered, duplicates, and invalid _entries
        var points = new List<(DateOnly EffectiveAt, Guid CodeId, string? Reference)>
        {
            (default, code30, "invalid"),
            (d3, code30, "first"),
            (d2, code30, "d2"),
            (d3, vdr, "last"), // overrides previous d3
            (d2, Guid.Empty, "invalid")
        };

        await writer.ApplyChangePointsAsync(personId, points, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        await using var ctx = db.Factory.CreateDbContext();
        var active = await ctx.TimesheetEntries
            .AsNoTracking()
            .Where(x => x.PersonId == personId && !x.IsDeleted)
            .OrderBy(x => x.From)
            .ToListAsync();

        // d0 (enroll) + d2 + d3
        Assert.Equal(3, active.Count);
        Assert.Equal(d0, active[0].From);
        Assert.Equal(d2, active[0].To);
        Assert.Equal(d2, active[1].From);
        Assert.Equal(d3, active[1].To);
        Assert.Equal(code30, active[1].TimesheetCodeDefinitionId);
        Assert.Equal("d2", active[1].Reference);

        Assert.Equal(d3, active[2].From);
        Assert.Null(active[2].To);
        Assert.Equal(vdr, active[2].TimesheetCodeDefinitionId);
        Assert.Equal("last", active[2].Reference);
    }

    [Fact]
    public async Task UpdateAsync_UsesAuditAndCorrectsEntry()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var epRepo = new TimesheetEpisodeRepository(db.Factory);
        var writer = new TimesheetEntryWriterRepository(db.Factory);

        var personId = Guid.NewGuid();
        var d0 = new DateOnly(2026, 02, 10);
        await epRepo.OpenOnEnrollAsync(personId, d0, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        var code30 = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "30");
        var d3 = d0.AddDays(3);
        var id = await writer.AddEntryAsync(personId, d3, code30, reference: "r", note: null,
            TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        await using var ctx0 = db.Factory.CreateDbContext();
        var row = await ctx0.TimesheetEntries.AsNoTracking().SingleAsync(x => x.Id == id);

        var vdr = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "ВДР");

        row.TimesheetCodeDefinitionId = vdr;
        row.Reference = "new";
        row.UpdatedBy = TimesheetRepoTestHelpers.Author;
        row.UpdatedAtUtc = TimesheetRepoTestHelpers.NowUtc;

        await writer.UpdateAsync(row);

        await using var ctx = db.Factory.CreateDbContext();
        var updated = await ctx.TimesheetEntries.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(vdr, updated.TimesheetCodeDefinitionId);
        Assert.Equal("new", updated.Reference);
        Assert.Equal(TimesheetRepoTestHelpers.Author, updated.UpdatedBy);
        Assert.Equal(TimesheetRepoTestHelpers.NowUtc, updated.UpdatedAtUtc);
    }

    [Fact]
    public async Task SaveTransitionAsync_TouchesAuditAndAddsEntryWithProvidedId()
    {
        await using var db = new SqliteTestDb();
        await TimesheetRepoTestHelpers.SeedPolicyAsync(db);

        var epRepo = new TimesheetEpisodeRepository(db.Factory);
        var writer = new TimesheetEntryWriterRepository(db.Factory);

        var personId = Guid.NewGuid();
        var d0 = new DateOnly(2026, 02, 10);
        await epRepo.OpenOnEnrollAsync(personId, d0, TimesheetRepoTestHelpers.Author, TimesheetRepoTestHelpers.NowUtc);

        await using var ctx0 = db.Factory.CreateDbContext();
        var prev = await ctx0.TimesheetEntries.AsNoTracking()
            .Where(x => x.PersonId == personId && !x.IsDeleted)
            .SingleAsync(x => x.From == d0);

        var code30 = await TimesheetRepoTestHelpers.GetCodeIdAsync(db, "30");
        var nextId = Guid.NewGuid();
        var d3 = d0.AddDays(3);

        prev.UpdatedBy = TimesheetRepoTestHelpers.Author;
        prev.UpdatedAtUtc = TimesheetRepoTestHelpers.NowUtc;

        var next = new eRaven.Domain.Entities.TimesheetEntry
        {
            Id = nextId,
            TimesheetId = prev.TimesheetId,
            PersonId = prev.PersonId,
            TimesheetCodeDefinitionId = code30,
            From = d3,
            Reference = "ref",
            Note = "note",
            CreatedBy = TimesheetRepoTestHelpers.Author,
            CreatedAtUtc = TimesheetRepoTestHelpers.NowUtc
        };

        await writer.SaveTransitionAsync(prev, next);

        await using var ctx = db.Factory.CreateDbContext();
        var _entries = await ctx.TimesheetEntries.AsNoTracking()
            .Where(x => x.PersonId == personId)
            .OrderBy(x => x.From)
            .ToListAsync();

        Assert.Equal(2, _entries.Count);
        Assert.Equal(d0, _entries[0].From);
        Assert.Equal(d3, _entries[0].To);
        Assert.Equal(nextId, _entries[1].Id);
        Assert.Equal(code30, _entries[1].TimesheetCodeDefinitionId);
        Assert.Equal("ref", _entries[1].Reference);
        Assert.Equal("note", _entries[1].Note);

        // prev audit should be touched
        Assert.Equal(TimesheetRepoTestHelpers.Author, _entries[0].UpdatedBy);
        Assert.Equal(TimesheetRepoTestHelpers.NowUtc, _entries[0].UpdatedAtUtc);
    }
}
