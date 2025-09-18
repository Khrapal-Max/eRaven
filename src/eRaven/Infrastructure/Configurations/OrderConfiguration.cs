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
        e.ToTable("orders");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id");

        e.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired();

        e.Property(x => x.EffectiveMomentUtc)
            .HasColumnName("effective_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        e.Property(x => x.Author)
            .HasColumnName("author")
            .HasMaxLength(128);

        e.Property(x => x.RecordedUtc)
            .HasColumnName("recorded_utc")
            .HasColumnType("timestamp with time zone");

        e.HasMany(x => x.Plans)
            .WithOne(p => p.Order)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}