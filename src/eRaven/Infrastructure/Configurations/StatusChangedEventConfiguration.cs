//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusChangedEventConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public class StatusChangedEventConfiguration : IEntityTypeConfiguration<StatusChangedEvent>
{
    public void Configure(EntityTypeBuilder<StatusChangedEvent> e)
    {
        e.ToTable("status_changed_events");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id");

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.Property(x => x.StatusKindId)
            .HasColumnName("status_kind_id")
            .IsRequired();

        e.Property(x => x.EffectiveAt)
            .HasColumnName("effective_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        e.Property(x => x.Sequence)
            .HasColumnName("sequence")
            .HasDefaultValue((short)0)
            .IsRequired();

        e.Property(x => x.Note)
            .HasColumnName("note")
            .HasMaxLength(512);

        e.Property(x => x.Author)
            .HasColumnName("author")
            .HasMaxLength(64)
            .HasDefaultValue("system");

        e.Property(x => x.RecordedAt)
            .HasColumnName("recorded_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        e.Property(x => x.SourceDocumentId)
            .HasColumnName("source_document_id");

        e.Property(x => x.SourceDocumentType)
            .HasColumnName("source_document_type")
            .HasMaxLength(64);

        // Індекси
        e.HasIndex(x => new { x.PersonId, x.EffectiveAt })
            .HasDatabaseName("ix_status_changed_events_person_effective");

        e.HasIndex(x => new { x.PersonId, x.EffectiveAt, x.Sequence })
            .HasDatabaseName("ix_status_changed_events_person_effective_seq");

        e.HasIndex(x => new { x.SourceDocumentType, x.SourceDocumentId })
            .HasDatabaseName("ix_status_changed_events_source_document");
    }
}
