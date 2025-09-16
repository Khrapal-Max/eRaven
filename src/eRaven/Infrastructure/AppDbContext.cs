//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AppDbContext
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderAction> OrderActions => Set<OrderAction>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<PersonPositionAssignment> PersonPositionAssignments => Set<PersonPositionAssignment>();
    public DbSet<PersonStatus> PersonStatuses => Set<PersonStatus>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanAction> PlanActions => Set<PlanAction>();
    public DbSet<PlanServiceOptions> PlanServiceOptions => Set<PlanServiceOptions>();
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
