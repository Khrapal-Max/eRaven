//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// TimesheetCodeDefinitionConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for <see cref="TimesheetCodeDefinition"/>.
/// </summary>
public sealed class TimesheetCodeDefinitionConfiguration : IEntityTypeConfiguration<TimesheetCodeDefinition>
{
    public void Configure(EntityTypeBuilder<TimesheetCodeDefinition> e)
    {
        e.ToTable("timesheet_codes");

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        e.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(32)
            .IsRequired();

        e.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(256)
            .IsRequired();

        e.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        e.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        e.Property(x => x.Priority)
            .HasColumnName("priority")
            .IsRequired();

        e.Property(x => x.IsTerminal)
            .HasColumnName("is_terminal")
            .IsRequired();

        e.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        e.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(64)
            .IsRequired();

        e.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        e.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(64);

        e.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        e.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("ux_timesheet_codes_code");

        e.HasIndex(x => x.IsActive)
            .HasDatabaseName("ix_timesheet_codes_active");
    }
}
