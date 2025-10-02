//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AppDbContext
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Events;
using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Агрегат
    public DbSet<PersonAggregate> Persons { get; set; }

    // Events (частина агрегату)
    public DbSet<StatusChangedEvent> StatusChangedEvents { get; set; }
    public DbSet<PositionAssignedEvent> PositionAssignedEvents { get; set; }
    public DbSet<PlanActionRecordedEvent> PlanActionRecordedEvents { get; set; }

    // Довідники
    public DbSet<PositionUnit> PositionUnits { get; set; }
    public DbSet<StatusKind> StatusKinds { get; set; }
    public DbSet<StatusTransition> StatusTransitions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Postgres розширення для темпоральних обмежень
        if (Database.IsNpgsql())
        {
            modelBuilder.HasPostgresExtension("btree_gist");
        }

        // Застосувати всі конфігурації з поточної збірки
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
