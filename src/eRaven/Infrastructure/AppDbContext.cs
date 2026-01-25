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
    // Persons 
    public DbSet<PersonReadModel> PersonRead { get; set; }
    public DbSet<PersonEventRecord> PersonEvents { get; set; }

    // Codes
    public DbSet<TimesheetCodeDefinition> TimesheetCodes { get; set; }
    public DbSet<TimesheetCodeTransition> TimesheetCodeTransitions { get; set; }

    // Timesheet
    public DbSet<TimesheetTimeline> TimesheetTimelines { get; set; }
    public DbSet<TimesheetEntry> TimesheetEntries { get; set; }

    // Combat Tasks
    public DbSet<CombatTaskPlanLine> CombatTaskPlanLine { get; set; }
    public DbSet<CombatTaskAssignment> CombatTaskAssignments { get; set; }
    public DbSet<CombatTaskPlanDocument> CombatTaskPlanDocuments { get; set; }

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
