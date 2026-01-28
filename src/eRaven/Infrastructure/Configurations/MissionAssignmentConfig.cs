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

        b.HasIndex(x => x.PersonId);
        b.HasIndex(x => x.MissionId);
        b.HasIndex(x => x.StartedAt);

        // ended_at >= started_at
        b.ToTable(t => t.HasCheckConstraint(
            "ck_mission_assignments_dates",
            "\"ended_at\" IS NULL OR \"ended_at\" >= \"started_at\""));

        // Якщо закінчили — маємо end_document_id та end_action_id
        b.ToTable(t => t.HasCheckConstraint(
            "ck_mission_assignments_end_links",
            "\"ended_at\" IS NULL OR (\"end_document_id\" IS NOT NULL AND \"end_action_id\" IS NOT NULL)"));

        // Якщо НЕ закінчили — end_document_id/end_action_id мають бути NULL
        b.ToTable(t => t.HasCheckConstraint(
            "ck_mission_assignments_open_links",
            "\"ended_at\" IS NOT NULL OR (\"end_document_id\" IS NULL AND \"end_action_id\" IS NULL)"));

        // 1 активне призначення на особу
        // (для PostgreSQL працює; для SQLite теж ок як partial index)
        b.HasIndex(x => x.PersonId)
            .IsUnique()
            .HasFilter("\"ended_at\" IS NULL");

        // FK-и (зазвичай Restrict, щоб не ламати історію)
        b.HasOne<Mission>()
            .WithMany()
            .HasForeignKey(x => x.MissionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<CombatTaskDocument>()
            .WithMany()
            .HasForeignKey(x => x.StartDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<CombatTaskDocument>()
            .WithMany()
            .HasForeignKey(x => x.EndDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<MissionAction>()
            .WithMany()
            .HasForeignKey(x => x.StartActionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<MissionAction>()
            .WithMany()
            .HasForeignKey(x => x.EndActionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}