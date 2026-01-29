//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionActionPersonConfig
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class MissionActionPersonConfig : IEntityTypeConfiguration<MissionActionPerson>
{
    public void Configure(EntityTypeBuilder<MissionActionPerson> b)
    {
        b.ToTable("mission_action_persons");

        // Снапшот “Action × Person” — природний ключ
        b.HasKey(x => new { x.ActionId, x.PersonId });

        b.Property(x => x.ActionId)
            .HasColumnName("action_id")
            .IsRequired();

        b.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        b.Property(x => x.RNOKPP)
            .HasColumnName("rnokpp")
            .HasMaxLength(20)
            .IsRequired();

        b.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(250)
            .IsRequired();

        b.Property(x => x.Rank)
            .HasColumnName("rank")
            .HasMaxLength(120)
            .IsRequired();

        b.Property(x => x.Position)
            .HasColumnName("position")
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.Weapon)
            .HasColumnName("weapon")
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.Callsign)
            .HasColumnName("callsign")
            .HasMaxLength(80)
            .IsRequired();

        // Часті вибірки:
        // - всі участі по ActionId (побудова рядка документа)
        // - історія по PersonId (звіт/аудит)
        b.HasIndex(x => x.ActionId);
        b.HasIndex(x => x.PersonId);

        // (Опційно) FK на MissionAction (каскад — якщо дія видаляється, снапшоти також)
        b.HasOne<MissionAction>()
            .WithMany()
            .HasForeignKey(x => x.ActionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
