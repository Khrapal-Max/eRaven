//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// MissionAssignmentConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class MissionAssignmentConfiguration : IEntityTypeConfiguration<MissionAssignment>
{
    public void Configure(EntityTypeBuilder<MissionAssignment> e)
    {
        e.ToTable("mission_assignments", t =>
        {
            t.HasCheckConstraint(
                "ck_mission_assignments_to_gte_from",
                "to_date IS NULL OR to_date >= from_date");

            t.HasCheckConstraint(
                "ck_mission_assignments_status",
                "status IN (0,1,2,3)");
        });

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id")
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

        e.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

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

        // Швидкі запити (додаємо Status, бо фільтруємо)
        e.HasIndex(x => new { x.MissionId, x.Status, x.From, x.To });
        e.HasIndex(x => new { x.PersonId, x.Status, x.From, x.To });

        // Ідемпотентність apply (один старт-рядок -> один assignment)
        e.HasIndex(x => x.SourceStartDetailsId).IsUnique();

        // 1 активний open-ended на особу (тільки для Planned/Committed)
        e.HasIndex(x => x.PersonId)
            .HasFilter("to_date IS NULL AND status IN (0,1)")
            .IsUnique();
    }
}
