//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetCodeTransitionConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class TimesheetCodeTransitionConfiguration : IEntityTypeConfiguration<TimesheetCodeTransition>
{
    public void Configure(EntityTypeBuilder<TimesheetCodeTransition> e)
    {
        e.ToTable("timesheet_code_ttransitions");

        e.HasKey(x => x.Id);

        e.Property(x => x.StartShiftDays)
            .HasColumnName("start_shift_days")
            .IsRequired();

        e.HasIndex(x => new { x.FromCodeId, x.ToCodeId })
            .IsUnique();

        e.HasOne(x => x.FromCode)
          .WithMany()
          .HasForeignKey(x => x.FromCodeId)
          .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.ToCode)
            .WithMany()
            .HasForeignKey(x => x.ToCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        // 0 або 1 (за поточною картою)
        e.ToTable(t =>
            t.HasCheckConstraint("CK_timesheet_code_ttransitions_start_shift_days", "\"start_shift_days\" IN (0,1)"));
    }
}