//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// CombatTaskDetailsConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for <see cref="CombatTaskDetails"/>.
/// </summary>
public sealed class CombatTaskDetailsConfiguration : IEntityTypeConfiguration<CombatTaskDetails>
{
    public void Configure(EntityTypeBuilder<CombatTaskDetails> e)
    {
        e.ToTable("combat_task_details");

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        e.Property(x => x.CombatTaskId)
            .HasColumnName("combat_task_id")
            .IsRequired();

        e.Property(x => x.Kind)
            .HasColumnName("kind")
            .HasConversion<int>()
            .IsRequired();

        e.Property(x => x.EffectiveAt)
            .HasColumnName("effective_at")
            .IsRequired();

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        // Snapshot fields
        e.Property(x => x.Rnokpp)
            .HasColumnName("rnokpp")
            .HasMaxLength(10)
            .IsRequired();

        e.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(512)
            .IsRequired();

        e.Property(x => x.Rank)
            .HasColumnName("rank")
            .HasMaxLength(128);

        e.Property(x => x.Position)
            .HasColumnName("position")
            .HasMaxLength(512);

        e.Property(x => x.Weapon)
            .HasColumnName("weapon")
            .HasMaxLength(128);

        e.Property(x => x.Callsign)
            .HasColumnName("callsign")
            .HasMaxLength(128);

        // Indexes
        e.HasIndex(x => x.CombatTaskId)
            .HasDatabaseName("ix_combat_task_details_task");

        e.HasIndex(x => new { x.PersonId, x.EffectiveAt })
            .HasDatabaseName("ix_combat_task_details_person_date");

        // FK
        e.HasOne(x => x.CombatTask)
            .WithMany(x => x.CombatTaskDetails)
            .HasForeignKey(x => x.CombatTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
