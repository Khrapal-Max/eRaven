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
            .HasMaxLength(120)
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

        // Порядок усередині документа — єдине джерело істини
        b.HasIndex(x => new { x.DocumentId, x.Sequence })
            .IsUnique();

        b.HasIndex(x => x.DocumentId);
        b.HasIndex(x => x.MissionId);
        b.HasIndex(x => x.ActionDate);

        // захист від 0/від’ємних sequence
        b.ToTable(t => t.HasCheckConstraint(
            "ck_mission_actions_sequence",
            "\"sequence\" >= 1"));

        // якщо в тебе є таблиця documents — FK (можна Restrict, щоб не видалити історію випадково)
        b.HasOne<CombatTaskDocument>()
            .WithMany()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        // якщо Mission теж в цій БД
        b.HasOne<Mission>()
            .WithMany()
            .HasForeignKey(x => x.MissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
