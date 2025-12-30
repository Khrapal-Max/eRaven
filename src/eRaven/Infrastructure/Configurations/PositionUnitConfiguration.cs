//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public class PositionUnitConfiguration : IEntityTypeConfiguration<PositionUnit>
{
    public void Configure(EntityTypeBuilder<PositionUnit> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("position_units");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
         .HasColumnName("id");

        // ===============================
        // Columns (lower snake_case)
        // ===============================
        e.Property(x => x.Number)
         .HasColumnName("number")
         .HasColumnType("int")
         .IsRequired();

        e.Property(x => x.Code)
         .HasColumnName("code")
         .HasMaxLength(64)
         .IsRequired();// nullable за доменною моделлю

        e.Property(x => x.ShortName)
         .HasColumnName("short_name")
         .HasMaxLength(128)
         .IsRequired();

        e.Property(x => x.FullName)
         .HasColumnName("full_name")
         .HasMaxLength(512)
         .IsRequired();

        e.Property(x => x.SpecialNumber)
         .HasColumnName("special_number")
         .HasMaxLength(15)
         .IsRequired();

        e.Property(x => x.Rank)
         .HasColumnName("rank")
         .HasMaxLength(128)
         .IsRequired();

        e.Property(x => x.Tarif)
         .HasColumnName("tarif")
         .HasMaxLength(5)
         .IsRequired();

        e.Property(x => x.IsActived)
         .HasColumnName("is_active")
         .HasDefaultValue(true)
         .IsRequired();

        // ===============================
        // Indexes
        // ===============================
        e.HasIndex(x => x.Code)
         .HasDatabaseName("ix_position_units_code")
         .IsUnique();

        e.HasIndex(x => x.ShortName)
         .HasDatabaseName("ix_position_units_short_name");

        e.HasIndex(x => x.SpecialNumber)
        .HasDatabaseName("ix_position_units_number");
    }
}