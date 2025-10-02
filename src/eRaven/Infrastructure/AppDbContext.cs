//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AppDbContext
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PersonAggregate> Persons {get; set; }
    public DbSet<PlanAction> PlanActions { get; set; }
    public DbSet<PersonStatus> PersonStatuses { get; set; }
    public DbSet<PersonPositionAssignment> PersonPositionAssignments { get; set; }
    public DbSet<PositionUnit> PositionUnits { get; set; }
    public DbSet<StatusKind> StatusKinds { get; set; }
    public DbSet<StatusTransition> StatusTransitions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Postgres extension лише для Npgsql
        if (Database.IsNpgsql())
            modelBuilder.HasPostgresExtension("btree_gist");

        // Підтягнути всі IEntityTypeConfiguration<> з поточної збірки
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
