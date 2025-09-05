/*//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlannedActionConfiguration
//-----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UI.Blazor.Domain.Models;

namespace UI.Blazor.Infrastructure.Configurations;

public sealed class PlannedActionConfiguration : IEntityTypeConfiguration<PlannedAction>
{
    public void Configure(EntityTypeBuilder<PlannedAction> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("planned_actions");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
         .HasColumnName("id");

        // ===============================
        // Columns (lower snake_case)
        // ===============================
        e.Property(x => x.PlanKey)
         .HasColumnName("plan_key")
         .IsRequired();

        e.Property(x => x.PersonId)
         .HasColumnName("person_id")
         .IsRequired();

        e.Property(x => x.MoveType)
         .HasColumnName("move_type")
         .HasConversion<int>()     // enum -> int
         .IsRequired();

        e.Property(x => x.ActionType)
         .HasColumnName("action_type")
         .HasConversion<int>()     // enum -> int
         .IsRequired();

        e.Property(x => x.ActionAt)
         .HasColumnName("action_at")
         .IsRequired()
         .HasDefaultValueSql("timezone('utc', now())");

        e.Property(x => x.RecordedAt)
         .HasColumnName("recorded_at")
         .IsRequired()
         .HasDefaultValueSql("timezone('utc', now())");

        e.Property(x => x.PositionSnapshot)
         .HasColumnName("position_snapshot")
         .HasMaxLength(512);

        e.Property(x => x.RankSnapshot)
         .HasColumnName("rank_snapshot")
         .HasMaxLength(128);

        e.Property(x => x.WeaponSnapshot)
         .HasColumnName("weapon_snapshot")
         .HasMaxLength(128);

        e.Property(x => x.CallsignSnapshot)
         .HasColumnName("callsign_snapshot")
         .HasMaxLength(64);

        e.Property(x => x.Location)
         .HasColumnName("location")
         .HasMaxLength(256);

        e.Property(x => x.Group)
         .HasColumnName("group_name")   // avoid reserved word
         .HasMaxLength(128);

        e.Property(x => x.Crew)
         .HasColumnName("crew")
         .HasMaxLength(128);

        e.Property(x => x.OpenDocumentName)
         .HasColumnName("open_document_name")
         .IsRequired()                  // за доменом — обов'язково
         .HasMaxLength(256);

        e.Property(x => x.CloseStatusKindId)
         .HasColumnName("close_status_kind_id");

        e.Property(x => x.CloseDocumentName)
         .HasColumnName("close_document_name")
         .HasMaxLength(256);

        e.Property(x => x.Author)
         .HasColumnName("author")
         .HasMaxLength(128);

        e.Property(x => x.Modified)
         .HasColumnName("modified")
         .HasDefaultValueSql("timezone('utc', now())");

        // ===============================
        // Relationships
        // ===============================
        e.HasOne(x => x.Person)
         .WithMany()
         .HasForeignKey(x => x.PersonId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.CloseStatusKind)
         .WithMany()
         .HasForeignKey(x => x.CloseStatusKindId)
         .OnDelete(DeleteBehavior.Restrict);

        // ===============================
        // Constraints & Indexes
        // ===============================
        // Унікальність типу дії в межах одного плану
        e.HasIndex(x => new { x.PersonId, x.PlanKey, x.ActionType })
         .HasDatabaseName("ux_planned_actions_person_plankey_actiontype")
         .IsUnique();

        // Швидкі фільтри
        e.HasIndex(x => new { x.PersonId, x.ActionAt })
         .HasDatabaseName("ix_planned_actions_person_action_at");

        e.HasIndex(x => x.PlanKey)
         .HasDatabaseName("ix_planned_actions_plan_key");

        // Заборонити більше одного Close на один plan_key
        e.HasIndex(x => x.PlanKey)
         .HasDatabaseName("ux_planned_actions_plan_key_close_only")
         .HasFilter("\"action_type\" = 2")
         .IsUnique();

        // CHECK: для Close обов'язково статус + документ
        e.ToTable(t => t.HasCheckConstraint(
            "ck_planned_actions_close_fields",
            "(\"action_type\" <> 2) OR (\"close_status_kind_id\" IS NOT NULL AND \"close_document_name\" IS NOT NULL)"
        ));

        // CHECK: дозволені статуси для Close: лише 1 ("В районі") або 2 ("В БР")
        e.ToTable(t => t.HasCheckConstraint(
            "ck_planned_actions_close_status_allowed",
            "(\"action_type\" <> 2) OR (\"close_status_kind_id\" IN (1,2))"
        ));
    }
}*/