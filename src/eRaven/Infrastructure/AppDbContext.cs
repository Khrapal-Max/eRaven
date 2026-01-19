//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AppDbContext
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Особа 
    public DbSet<PersonReadModel> PersonRead { get; set; }
    public DbSet<PersonEventRecord> PersonEvents { get; set; }

    // NEW:
    public DbSet<TimesheetEntry> TimesheetEntries { get; set; }
    public DbSet<MonthlyTimesheetReadModel> MonthlyTimesheet { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Postgres розширення для темпоральних обмежень
        if (Database.IsNpgsql())
        {
            modelBuilder.HasPostgresExtension("btree_gist");
        }

        // Застосувати всі конфігурації з поточної збірки
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.ConfigureProviderSpecificTypes(this);
    }
}
