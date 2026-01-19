//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntitiesSmokeTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Domain.Entities;

public sealed class TimesheetEntitiesSmokeTests
{
    [Fact]
    public void TimesheetEntry_should_have_expected_defaults()
    {
        var e = new TimesheetEntry();

        Assert.Equal(Guid.Empty, e.Id);
        Assert.Equal(Guid.Empty, e.PersonId);

        Assert.Equal(TimesheetLane.Main, e.Lane); // default enum = 0
        Assert.Equal(string.Empty, e.Code);

        Assert.Equal(default, e.From);
        Assert.Null(e.To);

        Assert.Null(e.Reference);
        Assert.Null(e.Note);

        Assert.Equal(string.Empty, e.CreatedBy);
        Assert.Equal(default, e.CreatedAtUtc);

        Assert.Null(e.UpdatedBy);
        Assert.Null(e.UpdatedAtUtc);

        Assert.False(e.IsDeleted);
        Assert.Null(e.DeletedBy);
        Assert.Null(e.DeletedAtUtc);
        Assert.Null(e.DeleteReason);
    }

    [Fact]
    public void TimesheetEntry_should_allow_setting_properties()
    {
        var id = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 01, 19, 10, 00, 00, DateTimeKind.Utc);

        var e = new TimesheetEntry
        {
            Id = id,
            PersonId = personId,
            Lane = TimesheetLane.Task,
            Code = "Ф100",
            From = new DateOnly(2026, 01, 10),
            To = new DateOnly(2026, 01, 12),
            Reference = "ref-1",
            Note = "note-1",
            CreatedBy = "system",
            CreatedAtUtc = nowUtc,
            UpdatedBy = "system2",
            UpdatedAtUtc = nowUtc.AddMinutes(1),
            IsDeleted = true,
            DeletedBy = "admin",
            DeletedAtUtc = nowUtc.AddMinutes(2),
            DeleteReason = "test"
        };

        Assert.Equal(id, e.Id);
        Assert.Equal(personId, e.PersonId);
        Assert.Equal(TimesheetLane.Task, e.Lane);
        Assert.Equal("Ф100", e.Code);
        Assert.Equal(new DateOnly(2026, 01, 10), e.From);
        Assert.Equal(new DateOnly(2026, 01, 12), e.To);
        Assert.Equal("ref-1", e.Reference);
        Assert.Equal("note-1", e.Note);
        Assert.Equal("system", e.CreatedBy);
        Assert.Equal(nowUtc, e.CreatedAtUtc);
        Assert.Equal("system2", e.UpdatedBy);
        Assert.Equal(nowUtc.AddMinutes(1), e.UpdatedAtUtc);
        Assert.True(e.IsDeleted);
        Assert.Equal("admin", e.DeletedBy);
        Assert.Equal(nowUtc.AddMinutes(2), e.DeletedAtUtc);
        Assert.Equal("test", e.DeleteReason);
    }

    [Fact]
    public async Task EF_should_persist_TimesheetEntry()
    {
        await using var testDb = new SqliteTestDb();
        using var db = testDb.Factory.CreateDbContext();

        var nowUtc = new DateTime(2026, 01, 19, 12, 00, 00, DateTimeKind.Utc);

        var entry = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            Lane = TimesheetLane.Main,
            Code = "30",
            From = new DateOnly(2026, 01, 01),
            To = null,
            Reference = null,
            Note = "InArea default not stored normally (test row)",
            CreatedBy = "system",
            CreatedAtUtc = nowUtc,
            IsDeleted = false
        };

        db.Set<TimesheetEntry>().Add(entry);
        await db.SaveChangesAsync();

        var loaded = await db.Set<TimesheetEntry>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == entry.Id);

        Assert.Equal(entry.PersonId, loaded.PersonId);
        Assert.Equal(TimesheetLane.Main, loaded.Lane);
        Assert.Equal("30", loaded.Code);
        Assert.Equal(new DateOnly(2026, 01, 01), loaded.From);
        Assert.Null(loaded.To);
        Assert.Equal("system", loaded.CreatedBy);
        Assert.Equal(nowUtc, loaded.CreatedAtUtc);
        Assert.False(loaded.IsDeleted);
    }
}
