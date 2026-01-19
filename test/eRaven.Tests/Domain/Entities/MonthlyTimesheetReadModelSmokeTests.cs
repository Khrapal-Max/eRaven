//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MonthlyTimesheetReadModelSmokeTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Domain.Entities;

public class MonthlyTimesheetReadModelSmokeTests
{
    [Fact]
    public void MonthlyTimesheetReadModel_should_have_expected_defaults()
    {
        var m = new MonthlyTimesheetReadModel();

        Assert.Equal(Guid.Empty, m.PersonId);
        Assert.Equal(0, m.Year);
        Assert.Equal(0, m.Month);

        Assert.Equal("[]", m.DaysJson);
        Assert.Equal(0, m.Version);
        Assert.Equal(default, m.UpdatedAtUtc);
    }



    [Fact]
    public async Task EF_should_persist_MonthlyTimesheetReadModel()
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
            DaysJson = "[]",
            Version = 1,
            UpdatedAtUtc = nowUtc
        };

        db.Set<MonthlyTimesheetReadModel>().Add(rm);
        await db.SaveChangesAsync();

        var loaded = await db.Set<MonthlyTimesheetReadModel>()
            .AsNoTracking()
            .SingleAsync(x => x.PersonId == personId && x.Year == 2026 && x.Month == 1);

        Assert.Equal("[]", loaded.DaysJson);
        Assert.Equal(1, loaded.Version);
        Assert.Equal(nowUtc, loaded.UpdatedAtUtc);
    }
}
