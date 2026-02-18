//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// CombatTaskDocumentConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for <see cref="CombatTaskDocument"/>.
/// </summary>
public sealed class CombatTaskDocumentConfiguration : IEntityTypeConfiguration<CombatTaskDocument>
{
    public void Configure(EntityTypeBuilder<CombatTaskDocument> e)
    {
        e.ToTable("combat_task_documents");

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        e.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        e.Property(x => x.OrderTitle)
            .HasColumnName("order_title")
            .HasMaxLength(128)
            .IsRequired();

        e.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(512);

        e.Property(x => x.RecordedAt)
            .HasColumnName("recorded_at")
            .IsRequired();

        e.Property(x => x.CanceledReason)
            .HasColumnName("canceled_reason")
            .HasMaxLength(512);

        // Audit
        e.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(64)
            .IsRequired();

        e.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        e.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(64);

        e.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        e.Property(x => x.CanceledBy)
            .HasColumnName("canceled_by")
            .HasMaxLength(64);

        e.Property(x => x.CanceledAtUtc)
            .HasColumnName("canceled_at_utc");

        // Indexes
        e.HasIndex(x => x.OrderTitle)
            .HasDatabaseName("ix_combat_task_documents_order_title");

        e.HasIndex(x => x.RecordedAt)
            .HasDatabaseName("ix_combat_task_documents_recorded_at");

        e.HasIndex(x => x.Status)
            .HasDatabaseName("ix_combat_task_documents_status");

        e.HasMany(x => x.CombatTasks)
            .WithOne(x => x.CombatTaskDocument)
            .HasForeignKey(x => x.CombatTaskDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
