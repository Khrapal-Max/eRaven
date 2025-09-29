//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("persons");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");

        // ===============================
        // Columns (lower snake_case)
        // ===============================
        e.Property(x => x.Rnokpp)
            .HasColumnName("rnokpp")
            .HasMaxLength(10)
            .IsRequired();

        e.Property(x => x.Rank)
            .HasColumnName("rank")
            .HasMaxLength(64)
            .IsRequired();

        e.Property(x => x.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(128)
            .IsRequired();

        e.Property(x => x.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(128)
            .IsRequired();

        e.Property(x => x.MiddleName)
            .HasColumnName("middle_name")
            .HasMaxLength(128);

        e.Property(x => x.BZVP)
            .HasColumnName("bzvp")
            .HasMaxLength(50)
            .IsRequired();

        e.Property(x => x.Weapon)
            .HasColumnName("weapon")
            .HasMaxLength(128);

        e.Property(x => x.Callsign)
            .HasColumnName("callsign")
            .HasMaxLength(64);

        e.Property(x => x.PositionUnitId)
            .HasColumnName("position_unit_id");

        e.Property(x => x.StatusKindId)
            .HasColumnName("status_kind_id");

        e.Property(x => x.IsAttached)
            .HasColumnName("is_attached")
            .HasDefaultValue(false);

        e.Property(x => x.AttachedFromUnit)
            .HasColumnName("attached_from_unit")
            .HasMaxLength(256);

        e.Property(x => x.CreatedUtc)
            .HasColumnName("created_utc")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        e.Property(x => x.ModifiedUtc)
            .HasColumnName("modified_utc")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // ===============================
        // Indexes & Constraints
        // ===============================
        e.HasIndex(x => x.Rnokpp)
            .HasDatabaseName("ix_persons_rnokpp")
            .IsUnique();

        e.HasIndex(x => new { x.LastName, x.FirstName, x.MiddleName })
            .HasDatabaseName("ix_persons_fullname");

        e.HasIndex(x => x.PositionUnitId)
            .HasDatabaseName("ux_persons_position_unit_id_not_null")
            .IsUnique()
            .HasFilter("\"position_unit_id\" IS NOT NULL");

        // ===============================
        // Relationships
        // ===============================
        // Поточна посада: 1↔0..1
        e.HasOne(x => x.PositionUnit)
            .WithOne(u => u.CurrentPerson)
            .HasForeignKey<Person>(x => x.PositionUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        // Довідник статусів
        e.HasOne(x => x.StatusKind)
            .WithMany()
            .HasForeignKey(x => x.StatusKindId)
            .OnDelete(DeleteBehavior.SetNull);

        // Історія статусів (було)
        e.HasMany(x => x.StatusHistory)
            .WithOne(s => s.Person)
            .HasForeignKey(s => s.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Історія призначень на посади
        e.HasMany(x => x.PositionAssignments)
            .WithOne(a => a.Person)
            .HasForeignKey(a => a.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        // 🔵 НОВЕ: Планові дії ↔ Person (1↔N)
        e.HasMany(x => x.PlanActions)
            .WithOne(a => a.Person)
            .HasForeignKey(a => a.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Обчислюване поле
        e.Ignore(x => x.FullName);
        e.Ignore(x => x.CurrentAssignment);
        e.Ignore(x => x.CurrentStatus);
        e.Ignore(x => x.PendingEvents);

        e.Navigation(x => x.StatusHistory)
            .HasField("_statusHistory")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        e.Navigation(x => x.PositionAssignments)
            .HasField("_positionAssignments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        e.Navigation(x => x.PlanActions)
            .HasField("_planActions")            
        e.Navigation(nameof(Person.StatusHistory))
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        e.Navigation(nameof(Person.PositionAssignments))
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        e.Navigation(nameof(Person.PlanActions))
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
