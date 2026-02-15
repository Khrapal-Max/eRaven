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
            .IsRequired();

        e.Property(x => x.TimesheetId)
            .HasColumnName("timesheet_id")
            .IsRequired();

        // NEW (recommended)
        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.Property(x => x.CombatTaskDocumentId)
            .HasColumnName("combat_task_document_id")
            .IsRequired();

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
            .HasConversion<int>();

        e.Property(x => x.ClosedByCodeId)
            .HasColumnName("closed_by_code_id");

        e.Property(x => x.ClosedReference)
            .HasColumnName("closed_reference")
            .HasMaxLength(512);

        e.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(128)
            .IsRequired();

        e.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        /// <summary>
        /// Інваріант: один інтервал на особу для (Document, Mission).
        /// </summary>
        e.HasIndex(x => new { x.PersonId, x.CombatTaskDocumentId, x.MissionId })
            .IsUnique();

        // індекси під запити
        e.HasIndex(x => new { x.MissionId, x.FromDate, x.ToDate, x.Status });
        e.HasIndex(x => new { x.CombatTaskDocumentId, x.Status });
        e.HasIndex(x => new { x.PersonId, x.FromDate, x.ToDate });
    }
}