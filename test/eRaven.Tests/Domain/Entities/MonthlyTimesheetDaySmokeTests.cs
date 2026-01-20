//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MonthlyTimesheetDaySmokeTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Domain.Entities;

public sealed class MonthlyTimesheetDaySmokeTests
{
    [Fact]
    public void MonthlyTimesheetDay_should_have_expected_defaults()
    {
        var d = new MonthlyTimesheetDay();

        Assert.Null(d.EntryId);
        Assert.Equal(0, d.Day);
        Assert.Equal(default, d.Lane); // зазвичай 0 => Main, але залежить від enum
        Assert.Equal(string.Empty, d.Code);
    }

    [Fact]
    public void MonthlyTimesheetDay_should_allow_setting_properties()
    {
        var id = Guid.NewGuid();

        var d = new MonthlyTimesheetDay
        {
            EntryId = id,
            Day = 15,
            Lane = TimesheetLane.Task,
            Code = "RP-001"
        };

        Assert.Equal(id, d.EntryId);
        Assert.Equal(15, d.Day);
        Assert.Equal(TimesheetLane.Task, d.Lane);
        Assert.Equal("RP-001", d.Code);
    }

    [Fact]
    public async Task EF_should_persist_days_via_MonthlyTimesheetReadModel()
    {
        await using var testDb = new SqliteTestDb();
        await using var db = await testDb.Factory.CreateDbContextAsync();

        var personId = Guid.NewGuid();
        var entryMain = Guid.NewGuid();
        var entryTask = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 01, 19, 13, 00, 00, DateTimeKind.Utc);

        var rm = new MonthlyTimesheetReadModel
        {
            PersonId = personId,
            Year = 2026,
            Month = 1,
            Version = 1,
            UpdatedAtUtc = nowUtc
        };

        // Якщо Days не ініціалізований в моделі — цей тест впаде (і це добре: значить треба init)
        rm.Days.Add(new MonthlyTimesheetDay
        {
            EntryId = entryMain,
            Day = 1,
            Lane = TimesheetLane.Main,
            Code = "30"
        });

        rm.Days.Add(new MonthlyTimesheetDay
        {
            EntryId = entryTask,
            Day = 1,
            Lane = TimesheetLane.Task,
            Code = "RP-001"
        });

        db.Set<MonthlyTimesheetReadModel>().Add(rm);
        await db.SaveChangesAsync();

        var loaded = await db.Set<MonthlyTimesheetReadModel>()
            .AsNoTracking()
            .Include(x => x.Days)
            .SingleAsync(x => x.PersonId == personId && x.Year == 2026 && x.Month == 1);

        Assert.Equal(1, loaded.Version);
        Assert.Equal(nowUtc, loaded.UpdatedAtUtc);

        Assert.Equal(2, loaded.Days.Count);

        var main = loaded.Days.Single(x => x.Day == 1 && x.Lane == TimesheetLane.Main);
        Assert.Equal(entryMain, main.EntryId);
        Assert.Equal("30", main.Code);

        var task = loaded.Days.Single(x => x.Day == 1 && x.Lane == TimesheetLane.Task);
        Assert.Equal(entryTask, task.EntryId);
        Assert.Equal("RP-001", task.Code);
    }
}
