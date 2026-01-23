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

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.Property(x => x.Lane)
            .HasColumnName("lane")
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

        e.HasIndex(x => new { x.PersonId, x.Lane, x.ClosedAt })
            .HasDatabaseName("ix_ts_timeline_person_lane_closed");
    }
}
