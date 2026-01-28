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

public class CombatTaskDocumentConfig : IEntityTypeConfiguration<CombatTaskDocument>
{
    public void Configure(EntityTypeBuilder<CombatTaskDocument> b)
    {
        b.ToTable("combat_task_documents");
        b.HasKey(x => x.Id);

        b.Property(x => x.DocumentTitle)
            .HasColumnName("document_title")
            .HasMaxLength(250)
            .IsRequired();

        b.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        b.Property(x => x.RecordedAt)
            .HasColumnName("recorded_at")
            .IsRequired();

        b.Property(x => x.Order)
           .HasColumnName("order")
           .HasMaxLength(500);

        b.Property(x => x.CanceledReason)
            .HasColumnName("canceled_reason")
            .HasMaxLength(500);

        b.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(80)
            .IsRequired();

        b.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at")
            .IsRequired();

        b.Property(x => x.UpdatedBy)
            .HasColumnName("updates_by")
            .HasMaxLength(80);

        b.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at");

        b.Property(x => x.CanceledBy)
            .HasColumnName("cancel_by")
            .HasMaxLength(80);

        b.Property(x => x.CanceledAtUtc)
            .HasColumnName("canceled_at");

        // швидкі вибірки по реєстру документів / місяць
        b.HasIndex(x => new { x.RecordedAt, x.Status });
        b.HasIndex(x => x.RecordedAt);
    }
}