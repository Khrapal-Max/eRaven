//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonPositionAssignmentConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PersonPositionAssignmentConfiguration : IEntityTypeConfiguration<PersonPositionAssignment>
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

        e.Property(x => x.FromUtc)
         .HasColumnName("from_utc")
         .HasColumnType("timestamp with time zone")
         .IsRequired();

        e.Property(x => x.ToUtc)
         .HasColumnName("to_utc")
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
         .HasDefaultValueSql("now()")
         .IsRequired();

        // ===============================
        // Relationships
        // ===============================
        e.HasOne(x => x.Person)
         .WithMany(p => p.PositionAssignments)
         .HasForeignKey(x => x.PersonId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.PositionUnit)
         .WithMany() // історію на позиції не ведемо як колекцію
         .HasForeignKey(x => x.PositionUnitId)
         .OnDelete(DeleteBehavior.Restrict);

        // ===============================
        // Constraints & Indexes
        // ===============================
        // базова валідність інтервалу
        e.ToTable(t => t.HasCheckConstraint(
            "ck_person_position_assignments_dates",
            "(\"to_utc\" IS NULL) OR (\"to_utc\" > \"from_utc\")"
        ));

        // не більше одного активного запису на людину
        e.HasIndex(x => x.PersonId)
         .HasDatabaseName("ux_ppassign_person_open")
         .IsUnique()
         .HasFilter("\"to_utc\" IS NULL");

        // не більше одного активного запису на посаду
        e.HasIndex(x => x.PositionUnitId)
         .HasDatabaseName("ux_ppassign_position_open")
         .IsUnique()
         .HasFilter("\"to_utc\" IS NULL");

        // індекси для пошуку/звітування
        e.HasIndex(x => new { x.PersonId, x.FromUtc })
         .HasDatabaseName("ix_ppassign_person_from");

        e.HasIndex(x => new { x.PersonId, x.ToUtc })
         .HasDatabaseName("ix_ppassign_person_to");

        e.HasIndex(x => new { x.PositionUnitId, x.FromUtc })
         .HasDatabaseName("ix_ppassign_position_from");
    }
}
