//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonPositionAssignmentConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public class PersonPositionAssignmentConfiguration : IEntityTypeConfiguration<PersonPositionAssignment>
{
    public void Configure(EntityTypeBuilder<PersonPositionAssignment> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("person_position_assignments");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
         .HasColumnName("id");

        // ===============================
        // Columns (lower snake_case)
        // ===============================
        e.Property(x => x.PersonId)
         .HasColumnName("person_id")
         .IsRequired();

        e.Property(x => x.PositionUnitId)
         .HasColumnName("position_unit_id")
         .IsRequired();

        e.Property(x => x.OpenUtc)
         .HasColumnName("open_utc")
         .HasColumnType("timestamp with time zone")
         .IsRequired();

        e.Property(x => x.CloseUtc)
         .HasColumnName("close_utc")
         .HasColumnType("timestamp with time zone");

        e.Property(x => x.Note)
         .HasColumnName("note")
         .HasMaxLength(512);

        e.Property(x => x.Author)
         .HasColumnName("author")
         .HasMaxLength(64)
         .HasDefaultValue("system");

        e.Property(x => x.ModifiedUtc)
         .HasColumnName("modified_utc")
         .HasColumnType("timestamp with time zone")
         .HasDefaultValueSql("CURRENT_TIMESTAMP")
         .IsRequired();

        e.Ignore(x => x.IsActive);

        // ===============================
        // Relationships
        // ===============================
        e.HasOne(x => x.Person)
         .WithMany(p => p.PositionAssignments)
         .HasForeignKey(x => x.PersonId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.PositionUnit)
         .WithMany()
         .HasForeignKey(x => x.PositionUnitId)
         .OnDelete(DeleteBehavior.Restrict);

        // ===============================
        // Constraints & Indexes
        // ===============================

        // базова валідність інтервалу
        e.ToTable(t => t.HasCheckConstraint(
            "ck_person_position_assignments_dates",
            "(close_utc IS NULL) OR (close_utc > open_utc)"
        ));

        // не більше одного активного запису на людину
        e.HasIndex(x => x.PersonId)
         .HasDatabaseName("ux_ppassign_person_open")
         .IsUnique()
         .HasFilter("close_utc IS NULL");

        // не більше одного активного запису на посаду
        e.HasIndex(x => x.PositionUnitId)
         .HasDatabaseName("ux_ppassign_position_open")
         .IsUnique()
         .HasFilter("close_utc IS NULL");

        // індекси для пошуку/звітування
        e.HasIndex(x => new { x.PersonId, x.OpenUtc })
         .HasDatabaseName("ix_ppassign_person_open");

        e.HasIndex(x => new { x.PersonId, x.CloseUtc })
         .HasDatabaseName("ix_ppassign_person_close");

        e.HasIndex(x => new { x.PositionUnitId, x.OpenUtc })
         .HasDatabaseName("ix_ppassign_position_open");
    }
}
