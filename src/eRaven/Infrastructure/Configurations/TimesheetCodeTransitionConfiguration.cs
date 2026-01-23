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
        e.ToTable("timesheet_code_transitions");
        e.HasKey(x => x.Id);

        e.Property(x => x.Lane)
            .HasColumnName("lane")
            .IsRequired();

        e.Property(x => x.FromCodeId)
            .HasColumnName("from_code_id")
            .IsRequired();

        e.Property(x => x.ToCodeId)
            .HasColumnName("to_code_id")
            .IsRequired();

        e.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(64)
            .IsRequired();

        e.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        e.HasOne(x => x.FromCode)
            .WithMany()
            .HasForeignKey(x => x.FromCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.ToCode)
            .WithMany()
            .HasForeignKey(x => x.ToCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => new { x.FromCodeId, x.ToCodeId })
            .IsUnique()
            .HasDatabaseName("ux_ts_transitions_from_to");

        e.HasIndex(x => new { x.Lane, x.FromCodeId })
            .HasDatabaseName("ix_ts_transitions_lane_from");
    }
}