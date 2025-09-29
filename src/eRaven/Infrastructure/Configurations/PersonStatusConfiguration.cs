//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//----------------------------------------------------------------------------- 
// PersonStatusConfiguration 
//----------------------------------------------------------------------------- 

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public class PersonStatusConfiguration : IEntityTypeConfiguration<PersonStatus>
{
    public void Configure(EntityTypeBuilder<PersonStatus> e)
    {
        e.ToTable("person_statuses");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id");

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.Property(x => x.StatusKindId)
            .HasColumnName("status_kind_id")
            .IsRequired();

        e.Property(x => x.Sequence)
            .HasColumnName("sequence")
            .HasDefaultValue((short)0)
            .IsRequired();

        e.Property(x => x.OpenDate)
            .HasColumnName("open_date")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        e.Property(x => x.Note)
            .HasColumnName("note")
            .HasMaxLength(512);

        e.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        e.Property(x => x.Author)
            .HasColumnName("author")
            .HasMaxLength(64)
            .HasDefaultValue("system");

        e.Property(x => x.Modified)
            .HasColumnName("modified")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Джерело статусу (лише для планових дій або майбутніх типів документів)
        e.Property(x => x.SourceDocumentId)
            .HasColumnName("source_document_id");

        e.Property(x => x.SourceDocumentType)
            .HasColumnName("source_document_type")
            .HasMaxLength(64);

        e.HasOne(x => x.Person)
            .WithMany(p => p.StatusHistory)
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.StatusKind)
            .WithMany()
            .HasForeignKey(x => x.StatusKindId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        e.HasIndex(x => new { x.PersonId, x.OpenDate })
            .HasDatabaseName("ix_person_statuses_person_open");

        e.HasIndex(x => new { x.PersonId, x.OpenDate, x.Sequence })
            .IsUnique()
            .HasFilter("is_active = TRUE")
            .HasDatabaseName("ux_person_statuses_person_open_seq_active");

        e.HasIndex(x => new { x.PersonId, x.IsActive, x.OpenDate })
            .HasDatabaseName("ix_person_statuses_person_active_open");

        e.HasIndex(x => new { x.SourceDocumentType, x.SourceDocumentId })
            .HasDatabaseName("ix_person_statuses_source_document");
    }
}
