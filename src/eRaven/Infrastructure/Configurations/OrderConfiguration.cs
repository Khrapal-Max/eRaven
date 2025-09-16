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

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("orders");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");

        // ===============================
        // Columns
        // ===============================
        e.Property(x => x.Name)
         .HasColumnName("name")
         .HasMaxLength(64)
         .IsRequired();

        e.Property(x => x.EffectiveMomentUtc)
         .HasColumnName("effective_moment_utc")
         .HasColumnType("timestamp with time zone")
         .IsRequired();

        e.Property(x => x.Author)
         .HasColumnName("author")
         .HasMaxLength(128);

        e.Property(x => x.RecordedUtc)
         .HasColumnName("recorded_utc")
         .HasColumnType("timestamp with time zone")
         .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // ===============================
        // Indexes & Constraints
        // ===============================
        e.HasIndex(x => x.Name)
         .HasDatabaseName("ux_orders_name")
         .IsUnique();

        // ===============================
        // Relationships
        // ===============================
        // Order (one) -> Plans (many) — налаштовано у PlanConfiguration через FK у Plan
        // Order (one) -> OrderActions (many)
        e.HasMany(x => x.Actions)
         .WithOne(a => a.Order)
         .HasForeignKey(a => a.OrderId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}