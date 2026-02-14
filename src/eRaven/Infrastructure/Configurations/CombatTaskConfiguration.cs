//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// CombatTaskConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class CombatTaskConfiguration : IEntityTypeConfiguration<CombatTask>
{
    public void Configure(EntityTypeBuilder<CombatTask> e)
    {
        e.ToTable("combat_tasks");

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

        e.Property(x => x.SourceDocument)
          .HasColumnName("sourge_document")
          .HasMaxLength(30)
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
        e.HasIndex(x => x.CombatTaskDocumentId);
        e.HasIndex(x => x.MissionId);
    }
}
