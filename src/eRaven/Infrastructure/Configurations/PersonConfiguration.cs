/*//-----------------------------------------------------------------------------
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
        e.Property(x => x.Id)
         .HasColumnName("id");

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

        // ===============================
        // Indexes
        // ===============================
        e.HasIndex(x => x.Rnokpp)
         .HasDatabaseName("ix_persons_rnokpp")
         .IsUnique();

        e.HasIndex(x => new { x.LastName, x.FirstName, x.MiddleName })
         .HasDatabaseName("ix_persons_fullname");

        // ===============================
        // Relationships
        // ===============================
        // Опційна позиція: при видаленні — SetNull
        e.HasOne(x => x.PositionUnit)
         .WithMany(x => x.People)
         .HasForeignKey(x => x.PositionUnitId)
         .OnDelete(DeleteBehavior.SetNull);

        // Обов’язковий довідник статусів: заборонити каскад
        e.HasOne(x => x.StatusKind)
         .WithMany()
         .HasForeignKey(x => x.StatusKindId)
         .IsRequired()
         .OnDelete(DeleteBehavior.Restrict);

        // Примітка: FullName не мапимо — це обчислюване поле домену
        e.Ignore(x => x.FullName);
    }
}*/