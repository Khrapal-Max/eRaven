//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskPlanLineConfig
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class CombatTaskPlanLineConfig : IEntityTypeConfiguration<CombatTaskPlanLine>
{
    public void Configure(EntityTypeBuilder<CombatTaskPlanLine> b)
    {
        b.ToTable("combat_task_plan_lines");
        b.HasKey(x => x.Id);

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

        b.Property(x => x.Note)
            .HasMaxLength(500);

        // Для пошуку та вивантажень
        b.HasIndex(x => new { x.PersonId, x.ActionDate });
        b.HasIndex(x => x.AssignmentId);

        // Захист: в межах одного документа не даємо дубль "Start" для тієї ж особи
        // (якщо тобі треба дозволяти кілька строк на людину в документі — прибери)
        b.HasIndex(x => new { x.DocumentId, x.PersonId, x.Kind }).IsUnique();
    }
}