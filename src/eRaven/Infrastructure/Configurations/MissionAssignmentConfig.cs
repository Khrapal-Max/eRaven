//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionAssignmentConfig
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class MissionAssignmentConfig : IEntityTypeConfiguration<MissionAssignment>
{
    public void Configure(EntityTypeBuilder<MissionAssignment> b)
    {
        b.ToTable("mission_assignments");
        b.HasKey(x => x.Id);

        b.Property(x => x.MissionId)
            .HasColumnName("mission_id")
            .IsRequired();

        b.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        b.Property(x => x.StartedAt)
            .HasColumnName("started_at")
            .HasColumnType("date")
            .IsRequired();

        b.Property(x => x.EndedAt)
            .HasColumnName("ended_at")
            .HasColumnType("date");

        b.Property(x => x.StartDocumentId)
            .HasColumnName("start_document_id")
            .IsRequired();

        b.Property(x => x.EndDocumentId)
            .HasColumnName("end_document_id");

        b.Property(x => x.StartActionId)
            .HasColumnName("start_action_id")
            .IsRequired();

        b.Property(x => x.EndActionId)
            .HasColumnName("end_action_id");

        // Узгодженість дат інтервалу
        b.ToTable(t => t.HasCheckConstraint(
            "ck_mission_assignments_dates",
            "\"ended_at\" IS NULL OR \"ended_at\" >= \"started_at\""));

        // Головне правило: лише 1 активне (відкрите) призначення на 1 особу
        // Postgres partial unique index
        b.HasIndex(x => x.PersonId)
            .IsUnique()
            .HasFilter("\"ended_at\" IS NULL");

        // Корисні індекси для звітів/вибірок
        b.HasIndex(x => new { x.MissionId, x.StartedAt });
        b.HasIndex(x => new { x.PersonId, x.StartedAt });

        // (Опційно) FK (часто корисно для каскадів/цілісності)
        b.HasOne<MissionAction>()
            .WithMany()
            .HasForeignKey(x => x.StartActionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<MissionAction>()
            .WithMany()
            .HasForeignKey(x => x.EndActionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<CombatTaskDocument>()
            .WithMany()
            .HasForeignKey(x => x.StartDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<CombatTaskDocument>()
            .WithMany()
            .HasForeignKey(x => x.EndDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        // b.HasOne<Mission>().WithMany().HasForeignKey(x => x.MissionId).OnDelete(DeleteBehavior.Restrict);
    }
}
