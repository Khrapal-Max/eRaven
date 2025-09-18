//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanConfiguration (final; minimal)
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> e)
    {
        e.ToTable("plans");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id");

        e.Property(x => x.PlanNumber)
            .HasColumnName("plan_number")
            .HasMaxLength(64)
            .IsRequired();

        e.Property(x => x.State)
            .HasColumnName("state")
            .IsRequired();

        e.Property(x => x.OrderId)
            .HasColumnName("order_id");

        e.Property(x => x.Author)
            .HasColumnName("author")
            .HasMaxLength(128);

        e.Property(x => x.RecordedUtc)
            .HasColumnName("recorded_utc")
            .HasColumnType("timestamp with time zone");

        e.HasIndex(x => x.PlanNumber)
            .IsUnique()
            .HasDatabaseName("ux_plans_plan_number");

        e.HasMany(x => x.PlanActions)
            .WithOne(a => a.Plan)
            .HasForeignKey(a => a.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}