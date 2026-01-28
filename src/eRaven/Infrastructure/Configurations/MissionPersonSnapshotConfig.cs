//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionPersonSnapshotConfig
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class MissionPersonSnapshotConfig : IEntityTypeConfiguration<MissionPersonSnapshot>
{
    public void Configure(EntityTypeBuilder<MissionPersonSnapshot> b)
    {
        b.ToTable("mission_person_snapshots");
        b.HasKey(x => x.Id);

        b.Property(x => x.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        b.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        b.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("date")
            .IsRequired();

        b.Property(x => x.RNOKPP)
            .HasColumnName("rnokpp")
            .HasMaxLength(16)
            .IsRequired();

        b.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(512)
            .IsRequired();

        b.Property(x => x.Rank)
            .HasColumnName("rank")
            .HasMaxLength(80)
            .IsRequired();

        b.Property(x => x.Position)
            .HasColumnName("position")
            .HasMaxLength(512)
            .IsRequired();

        b.Property(x => x.Weapon)
            .HasColumnName("weapon")
            .HasMaxLength(120)
            .IsRequired();

        b.Property(x => x.Callsign)
            .HasColumnName("callsign")
            .HasMaxLength(80)
            .IsRequired();

        // 1 snapshot на 1 документ для 1 особи
        b.HasIndex(x => new { x.DocumentId, x.PersonId })
            .IsUnique();

        b.HasIndex(x => x.PersonId);

        b.HasOne<CombatTaskDocument>()
            .WithMany()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}