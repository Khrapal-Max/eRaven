//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// CombatTaskDetailsConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class CombatTaskDetailsConfiguration : IEntityTypeConfiguration<CombatTaskDetails>
{
    public void Configure(EntityTypeBuilder<CombatTaskDetails> e)
    {
        e.ToTable("combat_task_lines", t =>
        {
            t.HasCheckConstraint("ck_combat_task_lines_kind_valid", "kind IN (1, 2)");
        });

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        e.Property(x => x.CombatTaskId)
            .HasColumnName("combat_task_id")
            .IsRequired();

        e.Property(x => x.Kind)
            .HasColumnName("kind")
            .IsRequired();

        e.Property(x => x.EffectiveAt)
            .HasColumnName("effective_at")
            .IsRequired();

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        // light snapshot для звітів / ідентифікації
        e.Property(x => x.Rnokpp)
            .HasColumnName("rnokpp")
            .HasMaxLength(10)
            .IsRequired();

        e.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(512)
            .IsRequired();

        e.Property(x => x.Callsign)
            .HasColumnName("callsign")
            .HasMaxLength(128);

        // Швидкі вибірки
        e.HasIndex(x => x.CombatTaskId);
        e.HasIndex(x => new { x.PersonId, x.EffectiveAt });
        e.HasIndex(x => x.Kind);

        // Захист від дублю одного й того ж рядка
        e.HasIndex(x => new { x.CombatTaskId, x.PersonId, x.Kind, x.EffectiveAt })
            .IsUnique();
    }
}
