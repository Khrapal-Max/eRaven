//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionActionConfig
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class MissionActionConfig : IEntityTypeConfiguration<MissionAction>
{
    public void Configure(EntityTypeBuilder<MissionAction> b)
    {
        b.ToTable("mission_actions");
        b.HasKey(x => x.Id);

        b.Property(x => x.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        b.Property(x => x.Sequence)
            .HasColumnName("sequence")
            .IsRequired();

        b.Property(x => x.SourceDocNo)
            .HasColumnName("source_doc_no")
            .HasMaxLength(250)
            .IsRequired();

        b.Property(x => x.Action)
            .HasColumnName("action_kind")
            .HasConversion<int>()
            .IsRequired();

        b.Property(x => x.MissionId)
            .HasColumnName("mission_id")
            .IsRequired();

        b.Property(x => x.ActionDate)
            .HasColumnName("action_date")
            .HasColumnType("date")
            .IsRequired();

        // Порядок дій у документі: sequence унікальний в межах DocumentId.
        b.HasIndex(x => new { x.DocumentId, x.Sequence })
            .IsUnique();

        // Фільтри/побудова “стрічки” документа
        b.HasIndex(x => new { x.DocumentId, x.ActionDate });

        // (Опційно) зв'язки (якщо хочеш навігації — додай властивості в сутності)
        // b.HasOne<CombatTaskDocument>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        // b.HasOne<Mission>().WithMany().HasForeignKey(x => x.MissionId).OnDelete(DeleteBehavior.Restrict);
    }
}
