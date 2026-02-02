//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetCodeDefinitionConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

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
            .HasMaxLength(128)
            .IsRequired();

        e.Property(x => x.EndMode)
            .HasColumnName("end_mode")
            .IsRequired();

        e.Property(x => x.EndDateMeaning)
            .HasColumnName("end_date_meaning")
            .IsRequired();

        e.Property(x => x.NextCodeOnEnd)
            .HasColumnName("next_code_on_end")
            .HasMaxLength(32);

        e.Property(x => x.IsPlanningCutoff)
            .HasColumnName("is_planning_cutoff")
            .IsRequired();

        e.Property(x => x.PlanningCutoffShiftDays)
            .HasColumnName("planning_cutoff_shift_days")
            .IsRequired();

        e.Property(x => x.IsTerminal)
            .HasColumnName("is_terminal")
            .IsRequired();

        e.Property(x => x.RequiresReference)
            .HasColumnName("requires_reference")
            .IsRequired();

        e.Property(x => x.RequiresNote)
            .HasColumnName("requires_note")
            .IsRequired();

        e.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
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
            .HasDatabaseName("ux_ts_codes_code");
    }
}
