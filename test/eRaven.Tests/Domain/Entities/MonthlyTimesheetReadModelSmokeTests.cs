//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MonthlyTimesheetReadModelSmokeTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Domain.Entities;

public sealed class MonthlyTimesheetReadModelSmokeTests
{
    [Fact]
    public void MonthlyTimesheetReadModel_should_have_expected_defaults()
    {
        var m = new MonthlyTimesheetReadModel();

        Assert.Equal(Guid.Empty, m.PersonId);
        Assert.Equal(0, m.Year);
        Assert.Equal(0, m.Month);

        Assert.NotNull(m.Days);
        Assert.Empty(m.Days);

        Assert.Equal(0, m.Version);
        Assert.Equal(default, m.UpdatedAtUtc);
    }

    [Fact]
    public async Task EF_should_persist_MonthlyTimesheetReadModel_with_days()
    {
        await using var testDb = new SqliteTestDb();
        using var db = testDb.Factory.CreateDbContext();

        var personId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 01, 19, 13, 00, 00, DateTimeKind.Utc);

        var rm = new MonthlyTimesheetReadModel
        {
            PersonId = personId,
            Year = 2026,
            Month = 1,
            Version = 1,
            UpdatedAtUtc = nowUtc,
            Days =
            [
                new MonthlyTimesheetDay
                {
                    Day = 1,
                    Lane = TimesheetLane.Main,
                    Code = "30",
                    EntryId = Guid.NewGuid()
                },
                new MonthlyTimesheetDay
                {
                    Day = 1,
                    Lane = TimesheetLane.Task,
                    Code = "",
                    EntryId = null
                },
                new MonthlyTimesheetDay
                {
                    Day = 2,
                    Lane = TimesheetLane.Main,
                    Code = "100",
                    EntryId = Guid.NewGuid()
                }
            ]
        };

        db.Set<MonthlyTimesheetReadModel>().Add(rm);
        await db.SaveChangesAsync();

        var loaded = await db.Set<MonthlyTimesheetReadModel>()
            .AsNoTracking()
            .Include(x => x.Days)
            .SingleAsync(x => x.PersonId == personId && x.Year == 2026 && x.Month == 1);

        Assert.Equal(1, loaded.Version);
        Assert.Equal(nowUtc, loaded.UpdatedAtUtc);

        Assert.NotNull(loaded.Days);
        Assert.Equal(3, loaded.Days.Count);

        var d1Main = loaded.Days.Single(x => x.Day == 1 && x.Lane == TimesheetLane.Main);
        Assert.Equal("30", d1Main.Code);
        Assert.NotNull(d1Main.EntryId);

        var d1Task = loaded.Days.Single(x => x.Day == 1 && x.Lane == TimesheetLane.Task);
        Assert.Equal("", d1Task.Code);
        Assert.Null(d1Task.EntryId);

        var d2Main = loaded.Days.Single(x => x.Day == 2 && x.Lane == TimesheetLane.Main);
        Assert.Equal("100", d2Main.Code);
        Assert.NotNull(d2Main.EntryId);
    }
}
