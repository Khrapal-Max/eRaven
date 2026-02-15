//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTaskSpanConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class TimesheetTaskSpanConfiguration : IEntityTypeConfiguration<TimesheetTaskSpan>
{
    public void Configure(EntityTypeBuilder<TimesheetTaskSpan> e)
    {
        e.ToTable("timesheet_task_spans");

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        e.Property(x => x.TimesheetId)
            .HasColumnName("timesheet_id")
            .IsRequired();

        e.Property(x => x.CombatTaskDocumentId)
            .HasColumnName("combat_task_document_id")
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

        e.HasIndex(x => new { x.TimesheetId, x.CombatTaskDocumentId })
            .IsUnique();

        e.HasIndex(x => x.CombatTaskDocumentId);

        e.HasIndex(x => new { x.TimesheetId, x.FromDate, x.ToDate });
    }
}
