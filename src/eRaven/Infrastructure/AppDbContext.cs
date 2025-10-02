//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AppDbContext
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Models;
using eRaven.Domain.Person;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PersonAggregate> Persons => Set<PersonAggregate>();
    public DbSet<PersonPositionAssignment> PersonPositionAssignments => Set<PersonPositionAssignment>();
    public DbSet<PersonStatus> PersonStatuses => Set<PersonStatus>();
    public DbSet<PlanAction> PlanActions => Set<PlanAction>();
    public DbSet<PositionUnit> PositionUnits => Set<PositionUnit>();
    public DbSet<StatusKind> StatusKinds => Set<StatusKind>();
    public DbSet<StatusTransition> StatusTransitions => Set<StatusTransition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Postgres extension лише для Npgsql
        if (Database.IsNpgsql())
            modelBuilder.HasPostgresExtension("btree_gist");

        // Підтягнути всі IEntityTypeConfiguration<> з поточної збірки
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
