//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
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
        e.Property(x => x.Code)
         .HasColumnName("code")
         .HasMaxLength(64)
         .IsRequired();// nullable за доменною моделлю

        e.Property(x => x.ShortName)
         .HasColumnName("short_name")
         .HasMaxLength(128)
         .IsRequired();

        e.Property(x => x.OrgPath)
         .HasColumnName("org_path")
         .HasMaxLength(512)
         .IsRequired();

        e.Property(x => x.SpecialNumber)
         .HasColumnName("special_number")
         .HasMaxLength(15)
         .IsRequired();

        e.Property(x => x.IsActived)
         .HasColumnName("is_active")
         .HasDefaultValue(true);

        // ===============================
        // Indexes
        // ===============================
        e.HasIndex(x => x.Code)
         .HasDatabaseName("ix_position_units_code");

        e.HasIndex(x => x.ShortName)
         .HasDatabaseName("ix_position_units_short_name");

        e.HasIndex(x => x.SpecialNumber)
        .HasDatabaseName("ix_position_units_number");

        // ===============================
        // Relationships
        // ===============================
        // Навігація CurrentPerson налаштована з боку Person:
        // PersonConfiguration: HasOne(p => p.PositionUnit).WithOne(u => u.CurrentPerson)...
        // Тут додатково нічого не визначаємо, щоб не дублювати.

        // ===============================
        // Ignored (computed)
        // ===============================
        e.Ignore(x => x.FullName);
    }
}