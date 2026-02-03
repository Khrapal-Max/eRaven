//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// CombatTaskDocumentConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class CombatTaskDocumentConfiguration : IEntityTypeConfiguration<CombatTaskDocument>
{
    public void Configure(EntityTypeBuilder<CombatTaskDocument> builder)
    {
        builder.ToTable("CombatTaskDocuments");

        builder.HasKey(x => x.Id);

        // ----------------------------
        // Properties
        // ----------------------------

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.OrderTitle)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.RecordedAt)
            .IsRequired();

        builder.Property(x => x.CanceledReason)
            .HasMaxLength(512);

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(64);

        builder.Property(x => x.CanceledBy)
            .HasMaxLength(64);

        // ----------------------------
        // Indexes (for lists / filters)
        // ----------------------------

        builder.HasIndex(x => x.RecordedAt);
        builder.HasIndex(x => x.Status);

        // Якщо OrderTitle справді унікальний в домені — лишаємо.
        // TODO: якщо унікальність залежить від підрозділу/року — зробити складений індекс.
        builder.HasIndex(x => x.OrderTitle)
            .IsUnique();

        // ----------------------------
        // Relationships
        // ----------------------------

        builder.HasMany(x => x.MissionParticipations)
            .WithOne(x => x.Document)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
