//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskPlanDocumentConfig
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class CombatTaskPlanDocumentConfig : IEntityTypeConfiguration<CombatTaskPlanDocument>
{
    public void Configure(EntityTypeBuilder<CombatTaskPlanDocument> b)
    {
        b.ToTable("combat_task_plan_documents");
        b.HasKey(x => x.Id);

        b.Property(x => x.PlanningDocTitle)
            .HasMaxLength(250)
            .IsRequired();

        b.Property(x => x.CreatedBy)
            .HasMaxLength(80)
            .IsRequired();

        b.Property(x => x.UpdatedBy)
            .HasMaxLength(80);

        b.Property(x => x.CanceledBy)
            .HasMaxLength(80);

        b.Property(x => x.CanceledReason)
            .HasMaxLength(500);

        b.HasMany(x => x.Lines)
            .WithOne(x => x.Document)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // швидкі вибірки по реєстру документів / місяць
        b.HasIndex(x => new { x.PlanningDate, x.Status });
        b.HasIndex(x => x.RecordedAt);
    }
}