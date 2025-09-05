/*//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitConfiguration
//-----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UI.Blazor.Domain.Models;

namespace UI.Blazor.Infrastructure.Configurations;

public sealed class PositionUnitConfiguration : IEntityTypeConfiguration<PositionUnit>
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
         .HasColumnName("code");

        e.Property(x => x.ShortName)
         .HasColumnName("short_name")
         .IsRequired()
         .HasMaxLength(128);

        e.Property(x => x.OrgPath)
         .HasColumnName("org_path")
         .HasMaxLength(512);

        e.Ignore(x => x.FullName);

        // ===============================
        // Relationships
        // ===============================
        e.HasMany(x => x.People)
         .WithOne(p => p.PositionUnit)
         .HasForeignKey(p => p.PositionUnitId)
         .OnDelete(DeleteBehavior.SetNull);

        // ===============================
        // Constraints & Indexes
        // ===============================
        // Унікальність "посада в межах шляху"
        e.HasIndex(x => new { x.OrgPath, x.ShortName, x.Code })
         .HasDatabaseName("ux_position_units_orgpath_shortname_code")
         .IsUnique();

        // Унікальний code (дозволяємо кілька NULL через фільтр)
        e.HasIndex(x => x.Code)
         .HasDatabaseName("ux_position_units_code")
         .IsUnique();

        // Додатковий індекс для пошуку за short_name
        e.HasIndex(x => x.ShortName)
         .HasDatabaseName("ix_position_units_short_name");
    }
}*/