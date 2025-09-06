//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// OrderConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("orders");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
         .HasColumnName("id");

        // ===============================
        // Columns (lower snake_case)
        // ===============================
        e.Property(x => x.PlanId)
         .HasColumnName("plan_id")
         .IsRequired();

        e.Property(x => x.Name)
         .HasColumnName("name")
         .HasMaxLength(128)
         .IsRequired();

        e.Property(x => x.EffectiveMomentUtc)
         .HasColumnName("effective_moment_utc")
         .HasColumnType("timestamp with time zone")
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
        // Relationships (1:1)
        // ===============================
        e.HasOne(x => x.Plan)
         .WithOne()
         .HasForeignKey<Order>(x => x.PlanId)
         .OnDelete(DeleteBehavior.Restrict);

        // ===============================
        // Constraints & Indexes
        // ===============================
        // один наказ ↔ один план
        e.HasIndex(x => x.PlanId)
         .HasDatabaseName("ux_orders_plan_id")
         .IsUnique();

        // пошук/звітність
        e.HasIndex(x => x.Name)
         .HasDatabaseName("ix_orders_name");

        e.HasIndex(x => x.EffectiveMomentUtc)
         .HasDatabaseName("ix_orders_effective_moment_utc");

        e.HasIndex(x => x.RecordedUtc)
         .HasDatabaseName("ix_orders_recorded_utc");

        // CHECK-и: тільки «назва не порожня». Квант 15 хв — перевіряємо в домені.
        e.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_orders_name_not_blank",
                "length(trim(name)) > 0"
            );
        });
    }
}
