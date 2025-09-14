//-----------------------------------------------------------------------------
// PlanElementConfiguration (final; з PersonId + анти-дубль індексом)
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PlanElementConfiguration : IEntityTypeConfiguration<PlanElement>
{
    public void Configure(EntityTypeBuilder<PlanElement> e)
    {
        // ---------------- Table & PK ----------------
        e.ToTable("plan_elements");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");

        // ---------------- Columns -------------------
        e.Property(x => x.PlanId)
         .HasColumnName("plan_id")
         .IsRequired();

        // 🔴 денормалізовано для швидких перевірок/індексів
        e.Property(x => x.PersonId)
         .HasColumnName("person_id")
         .IsRequired();

        e.Property(x => x.Type)
         .HasColumnName("type")
         .HasConversion<int>()
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

        e.Property(x => x.Author)
         .HasColumnName("author")
         .HasMaxLength(64)
         .HasDefaultValue("system");

        e.Property(x => x.RecordedUtc)
         .HasColumnName("recorded_utc")
         .HasColumnType("timestamp with time zone")
         .HasDefaultValueSql("CURRENT_TIMESTAMP")
         .IsRequired();

        // ---------------- Relationships -------------
        e.HasOne(x => x.Plan)
         .WithMany(p => p.PlanElements)
         .HasForeignKey(x => x.PlanId)
         .OnDelete(DeleteBehavior.Cascade);

        // 1:1 → PPS тримає FK (plan_element_id)
        e.HasOne(x => x.PlanParticipantSnapshot)
         .WithOne(p => p.PlanElement)
         .HasForeignKey<PlanParticipantSnapshot>(p => p.PlanElementId)
         .OnDelete(DeleteBehavior.Cascade);

        // ---------------- Indexes -------------------
        e.HasIndex(x => x.PlanId)
         .HasDatabaseName("ix_plan_elements_plan");

        e.HasIndex(x => new { x.PlanId, x.EventAtUtc })
         .HasDatabaseName("ix_plan_elements_plan_event");

        e.HasIndex(x => new { x.Type, x.EventAtUtc })
         .HasDatabaseName("ix_plan_elements_type_event");

        // 🔒 Анти-дубль: в межах плану та сама особа, той самий тип і момент
        e.HasIndex(x => new { x.PlanId, x.PersonId, x.Type, x.EventAtUtc })
         .IsUnique()
         .HasDatabaseName("ux_plan_elements_uni_moment");

        // (опціонально корисний для пошуків історії по особі в плані)
        e.HasIndex(x => new { x.PlanId, x.PersonId })
         .HasDatabaseName("ix_plan_elements_plan_person");
    }
}
