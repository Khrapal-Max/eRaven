//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskAssignmentConfig
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;


public sealed class CombatTaskAssignmentConfig : IEntityTypeConfiguration<CombatTaskAssignment>
{
    public void Configure(EntityTypeBuilder<CombatTaskAssignment> b)
    {
        b.ToTable("combat_task_assignments");
        b.HasKey(x => x.Id);

        b.Property(x => x.PlanningDocTitle)
            .HasMaxLength(250)
            .IsRequired();

        b.Property(x => x.PositionalArea)
            .HasMaxLength(140)
            .IsRequired();

        b.Property(x => x.GroupName)
            .HasMaxLength(140)
            .IsRequired();

        b.Property(x => x.AssetType)
            .HasMaxLength(120);

        b.Property(x => x.Goal)
            .HasMaxLength(160)
            .IsRequired();

        b.Property(x => x.RNOKPP)
            .HasMaxLength(20)
            .IsRequired();

        b.Property(x => x.FullName)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.Rank)
            .HasMaxLength(80);

        b.Property(x => x.Position)
            .HasMaxLength(200);

        b.Property(x => x.Weapon)
            .HasMaxLength(80);

        b.Property(x => x.Callsign)
            .HasMaxLength(80);

        b.Property(x => x.CreatedBy)
            .HasMaxLength(80)
            .IsRequired();

        b.Property(x => x.UpdatedBy)
            .HasMaxLength(80);

        // Basic check: EndedAt >= StartedAt (якщо EndedAt заданий)
        b.ToTable(t => t.HasCheckConstraint(
            "ck_combat_task_assignment_dates",
            "\"EndedAt\" IS NULL OR \"EndedAt\" >= \"StartedAt\""));

        // Швидкі вибірки: "хто на день", "план місяця"
        b.HasIndex(x => new { x.PlanningDate, x.PersonId });
        b.HasIndex(x => new { x.PersonId, x.StartedAt });

        // ГАРАНТІЯ: 1 активне завдання на людину
        // Postgres/Npgsql: partial unique index
        b.HasIndex(x => x.PersonId)
            .IsUnique()
            .HasFilter("\"EndedAt\" IS NULL");
    }
}