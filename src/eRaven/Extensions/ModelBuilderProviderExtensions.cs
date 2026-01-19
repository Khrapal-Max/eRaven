//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ModelBuilderProviderExtensions
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Extensions;

public static class ModelBuilderProviderExtensions
{
    public static void ConfigureProviderSpecificTypes(this ModelBuilder modelBuilder, DbContext db)
    {
        // PersonEventRecord.PayloadJson
        var personEvents = modelBuilder.Entity<PersonEventRecord>();

        var timesheets = modelBuilder.Entity<MonthlyTimesheetReadModel>();

        personEvents.Property(x => x.PayloadJson)
                    .HasColumnType(db.Database.IsNpgsql() ? "jsonb" : "TEXT");

        timesheets.Property(x => x.DaysJson)
                    .HasColumnType(db.Database.IsNpgsql() ? "jsonb" : "TEXT");
    }
}
