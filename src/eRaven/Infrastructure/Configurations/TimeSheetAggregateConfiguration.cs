//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//----------------------------------------------------------------------------- 
// TimesheetTimelineConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class TimeSheetAggregateConfiguration : IEntityTypeConfiguration<TimeSheetAggregate>
{
    public void Configure(EntityTypeBuilder<TimeSheetAggregate> e)
    {
        e.ToTable("timesheet_aggregates", t =>
        {
            // Hard invariant: closed_at must be >= opened_at (when present)
            t.HasCheckConstraint(
                "ck_ts_aggregates_closed_gte_opened",
                "closed_at IS NULL OR closed_at >= opened_at");
        });

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

        // Fast lookup by person + state
        e.HasIndex(x => new { x.PersonId, x.ClosedAt })
            .HasDatabaseName("ix_ts_aggregates_person_closed");

        // Fast lookup for "last closed episode" / ordering by opened
        e.HasIndex(x => new { x.PersonId, x.OpenedAt })
            .HasDatabaseName("ix_ts_aggregates_person_opened");

        // Guarantee: only one active timeline per person (ClosedAt == null)
        e.HasIndex(x => x.PersonId)
            .IsUnique()
            .HasFilter("closed_at IS NULL")
            .HasDatabaseName("ux_ts_aggregates_person_active");

        e.HasMany(x => x.Entries)
            .WithOne()
            .HasForeignKey(x => x.TimesheetId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasMany(x => x.TaskSpans)
            .WithOne()
            .HasForeignKey(x => x.TimesheetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}