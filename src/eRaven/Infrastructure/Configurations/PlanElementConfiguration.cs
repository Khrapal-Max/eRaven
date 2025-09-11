//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanElementConfiguration (final; без TimeKind; без снапшот-полів — вони у PPS)
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PlanElementConfiguration : IEntityTypeConfiguration<PlanElement>
{
    public void Configure(EntityTypeBuilder<PlanElement> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("plan_elements");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
         .HasColumnName("id");

        // ===============================
        // Columns (lower snake_case)
        // ===============================
        e.Property(x => x.PlanId)
         .HasColumnName("plan_id")
         .IsRequired();

        e.Property(x => x.Type)
         .HasColumnName("type")
         .HasConversion<int>() // enum -> int
         .IsRequired();

        e.Property(x => x.EventAtUtc)
         .HasColumnName("event_at_utc")
         .HasColumnType("timestamp with time zone")
         .IsRequired();

        e.Property(x => x.Location)
         .HasColumnName("location")
         .HasMaxLength(256);

        e.Property(x => x.GroupName)
         .HasColumnName("group_name")
         .HasMaxLength(128);

        e.Property(x => x.ToolType)
         .HasColumnName("tool_type")
         .HasMaxLength(128);

        e.Property(x => x.Note)
         .HasColumnName("note")
         .HasMaxLength(512);

        // --- аудит ---
        e.Property(x => x.Author)
         .HasColumnName("author")
         .HasMaxLength(64)
         .HasDefaultValue("system");

        e.Property(x => x.RecordedUtc)
         .HasColumnName("recorded_utc")
         .HasColumnType("timestamp with time zone")
         .HasDefaultValueSql("CURRENT_TIMESTAMP")
         .IsRequired();

        // ===============================
        // Relationships
        // ===============================
        e.HasOne(x => x.Plan)
         .WithMany(p => p.PlanElements)
         .HasForeignKey(x => x.PlanId)
         .OnDelete(DeleteBehavior.Cascade);

        // PlanElement 1 -> many PlanParticipantSnapshot (cascade)
        e.HasMany(x => x.Participants)
         .WithOne(p => p.PlanElement)
         .HasForeignKey(p => p.PlanElementId)
         .OnDelete(DeleteBehavior.Cascade);

        // ===============================
        // Indexes
        // ===============================
        e.HasIndex(x => x.PlanId)
         .HasDatabaseName("ix_plan_elements_plan");

        e.HasIndex(x => new { x.PlanId, x.EventAtUtc })
         .HasDatabaseName("ix_plan_elements_plan_event");

        e.HasIndex(x => new { x.Type, x.EventAtUtc })
         .HasDatabaseName("ix_plan_elements_type_event");

        // ===============================
        // Constraints (бізнес-інваріанти краще перевіряти у сервісі)
        // ===============================
        // Напр.: контроль кратності 15 хв для EventAtUtc робіть у домені/сервісі:
        // PlanElement.IsQuarterAligned / EnsureQuarterAligned
    }
}
