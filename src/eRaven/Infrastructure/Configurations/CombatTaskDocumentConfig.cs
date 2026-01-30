//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDocumentConfig
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class CombatTaskDocumentConfig : IEntityTypeConfiguration<CombatTaskDocument>
{
    public void Configure(EntityTypeBuilder<CombatTaskDocument> b)
    {
        b.ToTable("combat_task_documents");
        b.HasKey(x => x.Id);

        b.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        b.Property(x => x.OrderTitle)
            .HasColumnName("order_title")
            .HasMaxLength(250)
            .IsRequired();

        b.Property(x => x.RecordedAt)
            .HasColumnName("recorded_at")
            .HasColumnType("date")
            .IsRequired();

        b.Property(x => x.CanceledReason)
            .HasColumnName("canceled_reason")
            .HasMaxLength(500);

        b.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(120)
            .IsRequired();

        b.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        b.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(120);

        b.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        b.Property(x => x.CanceledBy)
            .HasColumnName("canceled_by")
            .HasMaxLength(120);

        b.Property(x => x.CanceledAtUtc)
            .HasColumnName("canceled_at_utc");

        b.HasIndex(x => x.OrderTitle).IsUnique();

        b.HasMany(x => x.CombatTasks)
            .WithOne(x => x.Document)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Узгодженість "Canceled": якщо status=2 (Canceled) — canceled_at_utc має бути
        b.ToTable(t => t.HasCheckConstraint(
            "ck_combat_task_documents_canceled",
            "status <> 2 OR canceled_at_utc IS NOT NULL"));
    }
}