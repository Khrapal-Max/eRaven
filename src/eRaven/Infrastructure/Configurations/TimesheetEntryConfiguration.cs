//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//----------------------------------------------------------------------------- 
// TimesheetEntryConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class TimesheetEntryConfiguration : IEntityTypeConfiguration<TimesheetEntry>
{
    public void Configure(EntityTypeBuilder<TimesheetEntry> e)
    {
        e.ToTable("timesheet_entries");
        e.HasKey(x => x.Id);

        e.Property(x => x.TimelineId)
            .HasColumnName("timeline_id")
            .IsRequired();

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.Property(x => x.Lane)
            .HasColumnName("lane")
            .IsRequired();

        e.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(32)
            .IsRequired();

        e.Property(x => x.From)
            .HasColumnName("from")
            .IsRequired();

        e.Property(x => x.To)
            .HasColumnName("to");

        e.Property(x => x.Reference)
            .HasColumnName("reference")
            .HasMaxLength(256);

        e.Property(x => x.Note)
            .HasColumnName("note")
            .HasMaxLength(1024);

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

        e.Property(x => x.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        e.Property(x => x.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(64);

        e.Property(x => x.DeletedAtUtc)
            .HasColumnName("deleted_at_utc");

        e.Property(x => x.DeleteReason)
            .HasColumnName("delete_reason")
            .HasMaxLength(512);

        e.HasOne(x => x.Timeline)
            .WithMany()
            .HasForeignKey(x => x.TimelineId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(x => new { x.PersonId, x.Lane, x.From, x.To })
            .HasDatabaseName("ix_ts_entries_person_lane_range");

        e.HasIndex(x => new { x.TimelineId, x.Lane, x.From })
            .HasDatabaseName("ix_ts_entries_timeline_lane_from");
    }
}
