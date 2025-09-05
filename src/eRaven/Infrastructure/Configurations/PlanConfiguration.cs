//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("plans");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
         .HasColumnName("id");

        // ===============================
        // Columns (lower snake_case)
        // ===============================
        e.Property(x => x.PlanNumber)
         .HasColumnName("plan_number")
         .HasMaxLength(64)
         .IsRequired();

        e.Property(x => x.Type)
         .HasColumnName("type")
         .HasConversion<int>()    // enum -> int
         .IsRequired();

        e.Property(x => x.PlannedAtUtc)
         .HasColumnName("planned_at_utc")
         .HasColumnType("timestamp with time zone")
         .IsRequired();

        e.Property(x => x.TimeKind)
         .HasColumnName("time_kind")
         .HasConversion<int>()    // enum -> int
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

        e.Property(x => x.TaskDescription)
         .HasColumnName("task_description")
         .HasMaxLength(1024);

        e.Property(x => x.State)
         .HasColumnName("state")
         .HasConversion<int>()
         .IsRequired();

        e.Property(x => x.Author)
         .HasColumnName("author")
         .HasMaxLength(64)
         .HasDefaultValue("system");

        e.Property(x => x.RecordedUtc)
         .HasColumnName("recorded_utc")
         .HasColumnType("timestamp with time zone")
         .HasDefaultValueSql("now()")
         .IsRequired();

        // ===============================
        // Relationships
        // ===============================
        // Plan -> Participants (snapshots): 1 -> many (Cascade on delete)
        e.HasMany(x => x.Participants)
         .WithOne()                       // у снапшота навігації на Plan немає
         .HasForeignKey("plan_id")        // FK-стовпець у таблиці snapshots
         .OnDelete(DeleteBehavior.Cascade);

        // ===============================
        // Constraints & Indexes
        // ===============================
        // Унікальний людський номер плану
        e.HasIndex(x => x.PlanNumber)
         .HasDatabaseName("ux_plans_plan_number")
         .IsUnique();

        // Пошукові індекси
        e.HasIndex(x => x.PlannedAtUtc)
         .HasDatabaseName("ix_plans_planned_at_utc");

        e.HasIndex(x => new { x.State, x.PlannedAtUtc })
         .HasDatabaseName("ix_plans_state_planned");

        e.HasIndex(x => new { x.Type, x.PlannedAtUtc })
         .HasDatabaseName("ix_plans_type_planned");

        // Назва не порожня; час кратний 15 хв і без секунд
        e.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_plans_plan_number_not_blank",
                "char_length(trim(plan_number)) > 0"
            );
            t.HasCheckConstraint(
                "ck_plans_planned_at_quarter",
                "(EXTRACT(MINUTE FROM planned_at_utc)::int % 15 = 0) AND EXTRACT(SECOND FROM planned_at_utc) = 0"
            );
        });

        // Домашні хелпери/властивості без мапінгу відсутні (EnsureQuarterAligned — метод)
    }
}
