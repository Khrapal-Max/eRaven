//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimeSheetAggregateConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for <see cref="TimeSheetAggregate"/> (timesheet episode root).
/// </summary>
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

        // Fast lookup for ordering by opened_at (last episode, etc.)
        e.HasIndex(x => new { x.PersonId, x.OpenedAt })
            .HasDatabaseName("ix_ts_aggregates_person_opened");

        // Guarantee: only one active episode per person (ClosedAt == null)
        e.HasIndex(x => x.PersonId)
            .IsUnique()
            .HasFilter("closed_at IS NULL")
            .HasDatabaseName("ux_ts_aggregates_person_active");

        // Entries: explicit navigation
        e.HasMany(x => x.Entries)
            .WithOne(x => x.TimeSheet)
            .HasForeignKey(x => x.TimesheetId)
            .OnDelete(DeleteBehavior.Cascade);

        // TaskSpans: no navigation in entity (owned by TimesheetId)
        e.HasMany(x => x.TaskSpans)
            .WithOne()
            .HasForeignKey(x => x.TimesheetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
