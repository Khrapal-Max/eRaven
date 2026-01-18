//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonEventRecordConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PersonEventRecordConfiguration : IEntityTypeConfiguration<PersonEventRecord>
{
    public void Configure(EntityTypeBuilder<PersonEventRecord> e)
    {
        e.ToTable("person_events");
        e.HasKey(x => x.EventId);

        e.Property(x => x.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        e.Property(x => x.AggregateId)
            .HasColumnName("aggregate_id")
            .IsRequired();

        e.Property(x => x.Version)
            .HasColumnName("version")
            .IsRequired();

        e.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(256)
            .IsRequired();

        // Базово: просто колонка "payload" як string
        e.Property(x => x.PayloadJson)
            .HasColumnName("payload")
            .IsRequired();

        e.Property(x => x.Author)
            .HasColumnName("author")
            .HasMaxLength(128)
            .IsRequired();

        e.Property(x => x.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        e.Property(x => x.EffectiveDate)
            .HasColumnName("effective_date");

        // ===== Indexes =====
        e.HasIndex(x => new { x.AggregateId, x.Version })
            .HasDatabaseName("ix_person_events_aggregate_version")
            .IsUnique();

        e.HasIndex(x => new { x.AggregateId, x.EffectiveDate })
            .HasDatabaseName("ix_person_events_aggregate_effective_date");

        e.HasIndex(x => x.EventType)
            .HasDatabaseName("ix_person_events_event_type");
    }
}
