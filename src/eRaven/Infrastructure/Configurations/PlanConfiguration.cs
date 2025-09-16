//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanConfiguration (final; minimal)
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("plans");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");

        // ===============================
        // Columns (lower snake_case)
        // ===============================
        e.Property(x => x.PlanNumber)
         .HasColumnName("plan_number")
         .HasMaxLength(64)
         .IsRequired();

        e.Property(x => x.State)
         .HasColumnName("state")
         .IsRequired();

        e.Property(x => x.Author)
         .HasColumnName("author")
         .HasMaxLength(128);

        e.Property(x => x.RecordedUtc)
         .HasColumnName("recorded_utc")
         .HasColumnType("timestamp with time zone")
         .HasDefaultValueSql("CURRENT_TIMESTAMP");

        e.Property(x => x.OrderId)
         .HasColumnName("order_id");

        // ===============================
        // Indexes & Constraints
        // ===============================
        e.HasIndex(x => x.PlanNumber)
         .HasDatabaseName("ux_plans_plan_number")
         .IsUnique();

        e.HasIndex(x => new { x.State, x.RecordedUtc })
         .HasDatabaseName("ix_plans_state_recorded");

        e.HasIndex(x => x.OrderId)
         .HasDatabaseName("ix_plans_order_id");

        // ===============================
        // Relationships
        // ===============================
        // Plan (many) -> Order (one), FK у Plan
        e.HasOne(x => x.Order)
         .WithMany(o => o.Plans)
         .HasForeignKey(x => x.OrderId)
         .OnDelete(DeleteBehavior.Restrict);

        // Навігація до PlanActions налаштовується у PlanActionConfiguration (1↔many)
    }
}