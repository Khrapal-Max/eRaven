//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskEntryConfig
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class CombatTaskEntryConfig : IEntityTypeConfiguration<CombatTaskEntry>
{
    public void Configure(EntityTypeBuilder<CombatTaskEntry> b)
    {
        b.ToTable("combat_task_entries");
        b.HasKey(x => x.Id);

        b.Property(x => x.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        b.Property(x => x.GroupId)
            .HasColumnName("group_id")
            .IsRequired();

        b.Property(x => x.GroupSequence)
            .HasColumnName("group_sequence")
            .IsRequired();

        b.Property(x => x.SourceDocNo)
            .HasColumnName("source_doc_no")
            .HasMaxLength(250)
            .IsRequired();

        b.Property(x => x.Action)
            .HasColumnName("action_kind")
            .HasConversion<int>()
            .IsRequired();

        b.Property(x => x.ActionDate)
            .HasColumnName("action_date")
            .HasColumnType("date")
            .IsRequired();

        b.Property(x => x.MissionId)
            .HasColumnName("mission_id")
            .IsRequired();

        b.Property(x => x.MissionDisplaySnapshot)
            .HasColumnName("mission_display")
            .HasMaxLength(600)
            .IsRequired();

        b.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        b.Property(x => x.RNOKPP)
            .HasColumnName("rnokpp")
            .HasMaxLength(10)
            .IsRequired();

        b.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(512)
            .IsRequired();

        b.Property(x => x.Rank)
            .HasColumnName("rank")
            .HasMaxLength(120)
            .IsRequired();

        b.Property(x => x.Position)
            .HasColumnName("position")
            .HasMaxLength(512)
            .IsRequired();

        b.Property(x => x.Weapon)
            .HasColumnName("weapon")
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.Callsign)
            .HasColumnName("callsign")
            .HasMaxLength(80)
            .IsRequired();

        // Один і той самий Person не може повторюватися в межах однієї групи (рядка)
        b.HasIndex(x => new { x.DocumentId, x.GroupId, x.PersonId }).IsUnique();

        // Для швидкого рендера документа: групи й порядок
        b.HasIndex(x => new { x.DocumentId, x.GroupSequence });
    }
}