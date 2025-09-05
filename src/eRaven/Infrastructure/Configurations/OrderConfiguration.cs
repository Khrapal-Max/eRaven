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
         .HasDefaultValueSql("timezone('utc', now())")
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

        // назва не порожня; момент прив'язуємо до кванту 15 хв і без секунд
        e.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_orders_name_not_blank",
                "char_length(trim(name)) > 0"
            );
            t.HasCheckConstraint(
                "ck_orders_effective_moment_quarter",
                "(EXTRACT(MINUTE FROM effective_moment_utc)::int % 15 = 0) AND EXTRACT(SECOND FROM effective_moment_utc) = 0"
            );
        });
    }
}
