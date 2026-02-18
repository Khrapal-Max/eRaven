//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTaskSpanConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for <see cref="TimesheetTaskSpan"/> (task fact / blocking interval).
/// NOTE: ToDate is treated as EXCLUSIVE bound: [FromDate..ToDate).
/// </summary>
public sealed class TimesheetTaskSpanConfiguration : IEntityTypeConfiguration<TimesheetTaskSpan>
{
    public void Configure(EntityTypeBuilder<TimesheetTaskSpan> e)
    {
        e.ToTable("timesheet_task_spans", t =>
        {
            t.HasCheckConstraint(
                "ck_ts_task_spans_to_gte_from",
                "to_date IS NULL OR to_date >= from_date");
        });

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired();

        e.Property(x => x.TimesheetId)
            .HasColumnName("timesheet_id")
            .IsRequired();

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.Property(x => x.OpenedByCombatTaskDocumentId)
            .HasColumnName("opened_by_combat_task_document_id")
            .IsRequired();

        e.Property(x => x.OpenedByDocumentReference)
            .HasColumnName("opened_by_document_reference")
            .HasMaxLength(512);

        e.Property(x => x.ClosedByCombatTaskDocumentId)
            .HasColumnName("closed_by_combat_task_document_id");

        e.Property(x => x.ClosedByDocumentReference)
            .HasColumnName("closed_by_document_reference")
            .HasMaxLength(512);

        e.Property(x => x.MissionId)
            .HasColumnName("mission_id")
            .IsRequired();

        e.Property(x => x.FromDate)
            .HasColumnName("from_date")
            .IsRequired();

        e.Property(x => x.ToDate)
            .HasColumnName("to_date");

        e.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        // Snapshot
        e.Property(x => x.Rnokpp)
            .HasColumnName("rnokpp")
            .HasMaxLength(10)
            .IsRequired();

        e.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(256)
            .IsRequired();

        e.Property(x => x.Rank)
            .HasColumnName("rank")
            .HasMaxLength(128);

        e.Property(x => x.Position)
            .HasColumnName("position")
            .HasMaxLength(256);

        e.Property(x => x.Weapon)
            .HasColumnName("weapon")
            .HasMaxLength(128);

        e.Property(x => x.Callsign)
            .HasColumnName("callsign")
            .HasMaxLength(128);

        // Close metadata
        e.Property(x => x.ClosedByCodeId)
            .HasColumnName("closed_by_code_id");

        e.Property(x => x.ClosedReference)
            .HasColumnName("closed_reference")
            .HasMaxLength(256);

        // Audit
        e.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(64)
            .IsRequired();

        e.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        e.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(64)
            .IsRequired();

        e.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        // ---- Indexes ----

        e.HasIndex(x => new { x.PersonId, x.FromDate, x.ToDate })
            .HasDatabaseName("ix_ts_task_spans_person_from_to");

        e.HasIndex(x => new { x.MissionId, x.FromDate, x.ToDate })
            .HasDatabaseName("ix_ts_task_spans_mission_from_to");

        e.HasIndex(x => new { x.OpenedByCombatTaskDocumentId, x.MissionId })
            .HasDatabaseName("ix_ts_task_spans_openedby_mission");

        e.HasIndex(x => x.ClosedByCombatTaskDocumentId)
            .HasDatabaseName("ix_ts_task_spans_closedby");

        e.HasIndex(x => new { x.PersonId, x.Status })
            .HasDatabaseName("ix_ts_task_spans_person_status");

        e.HasIndex(x => new { x.TimesheetId, x.OpenedByCombatTaskDocumentId, x.MissionId })
            .IsUnique()
            .HasDatabaseName("ux_ts_task_spans_timesheet_openedby_mission");
    }
}
