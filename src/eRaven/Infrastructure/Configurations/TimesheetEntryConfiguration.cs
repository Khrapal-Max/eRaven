//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntryConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class TimesheetEntryConfiguration : IEntityTypeConfiguration<TimesheetEntry>
{
    public void Configure(EntityTypeBuilder<TimesheetEntry> e)
    {
        e.ToTable("timesheet_entries", tb =>
        {
            tb.HasCheckConstraint(
                "ck_timesheet_entries_to_gt_from",
                "to_date IS NULL OR to_date > from_date");
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

        e.Property(x => x.TimesheetCodeDefinitionId)
            .HasColumnName("timesheet_codedefinition_id")
            .IsRequired();

        e.Property(x => x.From)
            .HasColumnName("from_date")
            .IsRequired();

        e.Property(x => x.To)
            .HasColumnName("to_date");

        e.Property(x => x.Reference)
            .HasColumnName("reference")
            // NOTE: Reference може містити перелік документів ("Doc1, Doc2, ...")
            .HasMaxLength(2048);

        e.Property(x => x.Note)
            .HasColumnName("note")
            .HasMaxLength(1024);

        e.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(64)
            .IsRequired();

        e.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        e.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(64);

        e.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        e.Property(x => x.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        e.Property(x => x.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(64);

        e.Property(x => x.DeletedAtUtc)
            .HasColumnName("deleted_at_utc");

        e.Property(x => x.DeleteReason)
            .HasColumnName("delete_reason")
            .HasMaxLength(512);

        e.HasOne(x => x.TimesheetCodeDefinition)
            .WithMany()
            .HasForeignKey(x => x.TimesheetCodeDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => new { x.PersonId, x.From, x.To })
            .HasDatabaseName("ix_ts_entries_person_range");

        // Invariant enforcement: один "активний" запис на дату (From) всередині епізоду.
        // Агрегат нормалізує Entries, але індекс додатково захищає від випадкових дублікатів.
        e.HasIndex(x => new { x.TimesheetId, x.From })
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("ux_ts_entries_timesheet_from_active");
    }
}
