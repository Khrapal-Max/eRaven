//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanConfiguration (final, minimal Plan model)
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> e)
    {
        // ===============================
        // Table & PK
        // ===============================
        e.ToTable("plans");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
         .HasColumnName("id");

        // ===============================
        // Columns (lower snake_case)
        // ===============================
        e.Property(x => x.PlanNumber)
         .HasColumnName("plan_number")
         .HasMaxLength(64)
         .IsRequired();

        e.Property(x => x.State)
         .HasColumnName("state")
         .HasConversion<int>()
         .IsRequired();

        e.Property(x => x.Author)
         .HasColumnName("author")
         .HasMaxLength(64)
         .HasDefaultValue("system");

        e.Property(x => x.RecordedUtc)
         .HasColumnName("recorded_utc")
         .HasColumnType("timestamp with time zone")
         .HasDefaultValueSql("CURRENT_TIMESTAMP")
         .IsRequired();

        // ===============================
        // Relationships
        // ===============================
        // Plan 1 -> many PlanElements (cascade delete)
        e.HasMany(p => p.PlanElements)
         .WithOne(pe => pe.Plan)
         .HasForeignKey(pe => pe.PlanId)
         .OnDelete(DeleteBehavior.Cascade);

        // ===============================
        // Indexes
        // ===============================
        e.HasIndex(x => x.PlanNumber)
         .HasDatabaseName("ux_plans_plan_number")
         .IsUnique();

        e.HasIndex(x => x.RecordedUtc)
         .HasDatabaseName("ix_plans_recorded_utc");

        e.HasIndex(x => new { x.State, x.RecordedUtc })
         .HasDatabaseName("ix_plans_state_recorded");

        // ===============================
        // Constraints
        // ===============================
        e.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_plans_plan_number_not_blank",
                "length(trim(plan_number)) > 0"
            );
        });
    }
}
