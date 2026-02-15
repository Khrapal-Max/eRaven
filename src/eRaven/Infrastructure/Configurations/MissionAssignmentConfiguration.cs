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
        });

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        e.Property(x => x.CombatTaskDocumentId)
            .HasColumnName("combat_task_document_id")
            .IsRequired();

        e.Property(x => x.MissionId)
            .HasColumnName("mission_id")
            .IsRequired();

        e.Property(x => x.PersonId)
            .HasColumnName("person_id").
            IsRequired();

        e.Property(x => x.From)
            .HasColumnName("from_date")
            .IsRequired();

        e.Property(x => x.To)
            .HasColumnName("to_date");

        e.Property(x => x.ClosedByDocumentId)
            .HasColumnName("closed_by_document_id");

        /// <summary>
        /// Інваріант: на один стартовий документ+місію+особу має бути максимум один факт.
        /// </summary>
        e.HasIndex(x => new { x.CombatTaskDocumentId, x.MissionId, x.PersonId })
            .IsUnique();

        // Для звітів
        e.HasIndex(x => new { x.MissionId, x.From, x.To });
        e.HasIndex(x => new { x.PersonId, x.From, x.To });

        /// <summary>
        /// Прискорює "закрити активний" (To IS NULL) по ключу документ+місія+особа.
        /// </summary>
        e.HasIndex(x => new { x.CombatTaskDocumentId, x.MissionId, x.PersonId })
            .HasFilter("to_date IS NULL");
    }
}