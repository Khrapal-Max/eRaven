//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// MissionAssignmentConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for <see cref="MissionAssignment"/>.
/// </summary>
public sealed class MissionAssignmentConfiguration : IEntityTypeConfiguration<MissionAssignment>
{
    public void Configure(EntityTypeBuilder<MissionAssignment> e)
    {
        e.ToTable("mission_assignments");

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired();

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.Property(x => x.MissionId)
            .HasColumnName("mission_id")
            .IsRequired();

        e.Property(x => x.From)
            .HasColumnName("from_date")
            .IsRequired();

        e.Property(x => x.To)
            .HasColumnName("to_date");

        e.Property(x => x.SourceStartDocumentId)
         .HasColumnName("source_start_document_id")
         .IsRequired();

        e.Property(x => x.SourceStartDetailsId)
            .HasColumnName("source_start_details_id")
            .IsRequired();

        e.Property(x => x.SourceEndDocumentId)
            .HasColumnName("source_end_document_id");

        e.Property(x => x.SourceEndDetailsId)
            .HasColumnName("source_end_details_id");

        e.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        e.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(128)
            .IsRequired();

        // Indexes: report & sync queries
        e.HasIndex(x => new { x.MissionId, x.PersonId, x.From })
            .HasDatabaseName("ix_mission_assignments_mission_person_from");

        // Apply/Cancel: all assignments started by a document in a mission
        e.HasIndex(x => new { x.SourceStartDocumentId, x.MissionId })
            .HasDatabaseName("ix_mission_assignments_startdoc_mission");

        // Apply/Cancel: all assignments closed by a document in a mission
        e.HasIndex(x => new { x.SourceEndDocumentId, x.MissionId })
            .HasDatabaseName("ix_mission_assignments_enddoc_mission");

        // Uniqueness: one start-assignment per (startDoc, mission, person, from)
        e.HasIndex(x => new { x.SourceStartDocumentId, x.MissionId, x.PersonId, x.From })
            .HasDatabaseName("ux_mission_assignments_startdoc_mission_person_from")
            .IsUnique();

        // Business invariant: one active (open-ended) task per person across all missions.
        // NOTE: PostgreSQL/SQLite support partial unique indexes. If your provider doesn't, keep this as a best-effort guard.
        e.HasIndex(x => x.PersonId)
            .HasDatabaseName("ux_mission_assignments_person_open")
            .IsUnique()
            .HasFilter("to_date IS NULL");
    }
}
