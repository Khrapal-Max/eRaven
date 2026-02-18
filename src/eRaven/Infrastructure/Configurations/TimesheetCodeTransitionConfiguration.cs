//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// TimesheetCodeTransitionConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for <see cref="TimesheetCodeTransition"/>.
/// </summary>
public sealed class TimesheetCodeTransitionConfiguration : IEntityTypeConfiguration<TimesheetCodeTransition>
{
    public void Configure(EntityTypeBuilder<TimesheetCodeTransition> e)
    {
        // FIX: було "timesheet_code_ttransitions" (опечатка)
        e.ToTable("timesheet_code_transitions", t =>
        {
            // 0 або 1 (за поточною картою)
            t.HasCheckConstraint(
                "ck_timesheet_code_transitions_start_shift_days",
                "\"start_shift_days\" IN (0,1)");
        });

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        e.Property(x => x.FromCodeId)
            .HasColumnName("from_code_id")
            .IsRequired();

        e.Property(x => x.ToCodeId)
            .HasColumnName("to_code_id")
            .IsRequired();

        e.Property(x => x.StartShiftDays)
            .HasColumnName("start_shift_days")
            .IsRequired();

        e.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(64)
            .IsRequired();

        e.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        e.HasIndex(x => new { x.FromCodeId, x.ToCodeId })
            .IsUnique()
            .HasDatabaseName("ux_timesheet_code_transitions_from_to");

        e.HasOne(x => x.FromCode)
            .WithMany()
            .HasForeignKey(x => x.FromCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.ToCode)
            .WithMany()
            .HasForeignKey(x => x.ToCodeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
