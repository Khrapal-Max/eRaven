//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionAssignedEventConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public class PositionAssignedEventConfiguration : IEntityTypeConfiguration<PositionAssignedEvent>
{
    public void Configure(EntityTypeBuilder<PositionAssignedEvent> e)
    {
        e.ToTable("position_assigned_events");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id");

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.Property(x => x.PositionUnitId)
            .HasColumnName("position_unit_id")
            .IsRequired();

        e.Property(x => x.OpenUtc)
            .HasColumnName("open_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        e.Property(x => x.CloseUtc)
            .HasColumnName("close_utc")
            .HasColumnType("timestamp with time zone");

        e.Property(x => x.Note)
            .HasColumnName("note").HasMaxLength(512);

        e.Property(x => x.Author)
            .HasColumnName("author")
            .HasMaxLength(64)
            .HasDefaultValue("system");

        e.Property(x => x.RecordedAt)
            .HasColumnName("recorded_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Обмеження
        e.ToTable(t => t.HasCheckConstraint(
            "ck_position_assigned_events_dates",
            "(close_utc IS NULL) OR (close_utc > open_utc)"));

        // Індекси
        e.HasIndex(x => x.PersonId)
            .HasDatabaseName("ux_position_assigned_events_person_open")
            .IsUnique()
            .HasFilter("close_utc IS NULL");

        e.HasIndex(x => x.PositionUnitId)
            .HasDatabaseName("ux_position_assigned_events_position_open")
            .IsUnique()
            .HasFilter("close_utc IS NULL");

        e.HasIndex(x => new { x.PersonId, x.OpenUtc })
            .HasDatabaseName("ix_position_assigned_events_person_open");

        e.HasIndex(x => new { x.PersonId, x.CloseUtc })
            .HasDatabaseName("ix_position_assigned_events_person_close");

        e.HasIndex(x => new { x.PositionUnitId, x.OpenUtc })
            .HasDatabaseName("ix_position_assigned_events_position_open");
    }
}
