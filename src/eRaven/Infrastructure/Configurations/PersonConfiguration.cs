//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
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
         .HasMaxLength(64);

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
         .HasMaxLength(50);

        e.Property(x => x.Weapon)
         .HasColumnName("weapon")
         .HasMaxLength(128);

        e.Property(x => x.Callsign)
         .HasColumnName("callsign")
         .HasMaxLength(64);

        e.Property(x => x.PositionUnitId)
         .HasColumnName("position_unit_id");

        e.Property(x => x.StatusKindId)
         .HasColumnName("status_kind_id")
         .HasDefaultValue(1); // дефолт = "В районі"

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

        // один Person на одну посаду (вакантні дозволені)
        e.HasIndex(x => x.PositionUnitId)
         .HasDatabaseName("ux_persons_position_unit_id_not_null")
         .IsUnique()
         .HasFilter("\"position_unit_id\" IS NOT NULL");

        // ===============================
        // Relationships
        // ===============================
        // Поточна посада: 1↔0..1 (FK лише в Person), при видаленні — SetNull
        e.HasOne(x => x.PositionUnit)
         .WithOne(u => u.CurrentPerson)
         .HasForeignKey<Person>(x => x.PositionUnitId)
         .OnDelete(DeleteBehavior.SetNull);

        // Обов’язковий довідник статусів: заборонити каскад
        e.HasOne(x => x.StatusKind)
         .WithMany()
         .HasForeignKey(x => x.StatusKindId)
         .IsRequired()
         .OnDelete(DeleteBehavior.Restrict);

        // FullName — обчислюване в домені, не мапимо
        e.Ignore(x => x.FullName);

        // Колекції історій (StatusHistory/PositionAssignments) конфігуруються у їхніх конфігураторах
    }
}