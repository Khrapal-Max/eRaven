//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// CombatTaskConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for <see cref="CombatTask"/>.
/// </summary>
public sealed class CombatTaskConfiguration : IEntityTypeConfiguration<CombatTask>
{
    public void Configure(EntityTypeBuilder<CombatTask> e)
    {
        e.ToTable("combat_tasks");

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired();

        e.Property(x => x.CombatTaskDocumentId)
            .HasColumnName("combat_task_document_id")
            .IsRequired();

        e.Property(x => x.MissionId)
            .HasColumnName("mission_id")
            .IsRequired();

        // FIX: було "sourge_document" (опечатка), уніфікуємо в snake_case.
        e.Property(x => x.SourceDocument)
            .HasColumnName("source_document")
            .HasMaxLength(128)
            .IsRequired();

        // Relations
        e.HasOne(x => x.CombatTaskDocument)
            .WithMany(d => d.CombatTasks)
            .HasForeignKey(x => x.CombatTaskDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasMany(x => x.CombatTaskDetails)
            .WithOne(x => x.CombatTask)
            .HasForeignKey(x => x.CombatTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Mission)
            .WithMany()
            .HasForeignKey(x => x.MissionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        e.HasIndex(x => x.CombatTaskDocumentId)
            .HasDatabaseName("ix_combat_tasks_document");

        e.HasIndex(x => x.MissionId)
            .HasDatabaseName("ix_combat_tasks_mission");
    }
}
