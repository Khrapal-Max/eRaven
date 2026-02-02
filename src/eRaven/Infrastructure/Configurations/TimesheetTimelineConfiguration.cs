//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//----------------------------------------------------------------------------- 
// TimesheetTimelineConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class TimesheetTimelineConfiguration : IEntityTypeConfiguration<TimesheetTimeline>
{
    public void Configure(EntityTypeBuilder<TimesheetTimeline> e)
    {
        e.ToTable("timesheet_timelines");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.Property(x => x.OpenedAt)
            .HasColumnName("opened_at")
            .IsRequired();

        e.Property(x => x.ClosedAt)
            .HasColumnName("closed_at");

        e.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(64)
            .IsRequired();

        e.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        e.Property(x => x.ClosedBy)
            .HasColumnName("closed_by")
            .HasMaxLength(64);

        e.Property(x => x.ClosedAtUtc)
            .HasColumnName("closed_at_utc");

        // 1) Fast lookup of timelines by person + state
        e.HasIndex(x => new { x.PersonId, x.ClosedAt })
            .HasDatabaseName("ix_ts_timelines_person_closed");

        // 2) Guarantee: only one active timeline per person (ClosedAt == null)
        // Npgsql supports filtered indexes.
        e.HasIndex(x => x.PersonId)
            .IsUnique()
            .HasFilter("closed_at IS NULL")
            .HasDatabaseName("ux_ts_timelines_person_active");

        // Navigation (optional): TimesheetTimeline.Entries
        e.HasMany(x => x.Entries)
            .WithOne(x => x.Timeline)
            .HasForeignKey(x => x.TimelineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
