//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// OrderActionConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public class OrderActionConfiguration : IEntityTypeConfiguration<OrderAction>
{
    public void Configure(EntityTypeBuilder<OrderAction> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("order_actions");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");

        // ===============================
        // Columns
        // ===============================
        e.Property(x => x.OrderId)
         .HasColumnName("order_id")
         .IsRequired();

        e.Property(x => x.PlanId)
         .HasColumnName("plan_id")
         .IsRequired();

        e.Property(x => x.PlanActionId)
         .HasColumnName("plan_action_id")
         .IsRequired();

        e.Property(x => x.PersonId)
         .HasColumnName("person_id")
         .IsRequired();

        e.Property(x => x.ActionType)
         .HasColumnName("action_type")
         .IsRequired();

        e.Property(x => x.EventAtUtc)
         .HasColumnName("event_at_utc")
         .HasColumnType("timestamp with time zone")
         .IsRequired();

        e.Property(x => x.Location)
         .HasColumnName("location")
         .HasMaxLength(128)
         .IsRequired();

        e.Property(x => x.GroupName)
         .HasColumnName("group_name")
         .HasMaxLength(128)
         .IsRequired();

        e.Property(x => x.CrewName)
         .HasColumnName("crew_name")
         .HasMaxLength(128)
         .IsRequired();

        // ------- Snapshot copy (from PlanAction) -------
        e.Property(x => x.Rnokpp)
         .HasColumnName("rnokpp")
         .HasMaxLength(10)
         .IsRequired();

        e.Property(x => x.FullName)
         .HasColumnName("full_name")
         .HasMaxLength(256)
         .IsRequired();

        e.Property(x => x.RankName)
         .HasColumnName("rank_name")
         .HasMaxLength(64)
         .IsRequired();

        e.Property(x => x.PositionName)
         .HasColumnName("position_name")
         .HasMaxLength(512)
         .IsRequired();

        e.Property(x => x.BZVP)
         .HasColumnName("bzvp")
         .HasMaxLength(50)
         .IsRequired();

        e.Property(x => x.Weapon)
         .HasColumnName("weapon")
         .HasMaxLength(128);

        e.Property(x => x.Callsign)
         .HasColumnName("callsign")
         .HasMaxLength(64);

        e.Property(x => x.StatusKindOnDate)
         .HasColumnName("status_kind_on_date")
         .HasMaxLength(64)
         .IsRequired();

        // ===============================
        // Indexes & Constraints
        // ===============================
        e.HasIndex(x => x.OrderId)
         .HasDatabaseName("ix_order_actions_order_id");

        e.HasIndex(x => new { x.OrderId, x.PersonId })
         .HasDatabaseName("ix_order_actions_order_person");

        // унікальність підтвердження однієї планової дії в наказі (захист від дублю)
        e.HasIndex(x => x.PlanActionId)
         .HasDatabaseName("ux_order_actions_plan_action_id")
         .IsUnique();

        // ===============================
        // Relationships
        // ===============================
        e.HasOne(x => x.Order)
         .WithMany(o => o.Actions)
         .HasForeignKey(x => x.OrderId)
         .OnDelete(DeleteBehavior.Restrict);

        // зв'язок з Person (для зручного join у звітах)
        e.HasOne(x => x.Person)
         .WithMany()
         .HasForeignKey(x => x.PersonId)
         .OnDelete(DeleteBehavior.Restrict);

        // опційні “безнавігаційні” зв'язки для трасування:
        e.HasOne<Plan>()
         .WithMany()
         .HasForeignKey(x => x.PlanId)
         .OnDelete(DeleteBehavior.Restrict);

        e.HasOne<PlanAction>()
         .WithMany()
         .HasForeignKey(x => x.PlanActionId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}