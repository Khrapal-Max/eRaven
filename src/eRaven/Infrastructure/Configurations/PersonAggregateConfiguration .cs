//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public class PersonAggregateConfiguration : IEntityTypeConfiguration<PersonAggregate>
{
    public void Configure(EntityTypeBuilder<PersonAggregate> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("persons");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");

        // ===== Value Objects (Owned Types) =====

        e.OwnsOne(x => x.PersonalInfo, pi =>
        {
            pi.Property(p => p.Rnokpp)
                .HasColumnName("rnokpp")
                .HasMaxLength(10)
                .IsRequired();

            pi.Property(p => p.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(128)
                .IsRequired();

            pi.Property(p => p.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(128)
                .IsRequired();

            pi.Property(p => p.MiddleName)
                .HasColumnName("middle_name")
                .HasMaxLength(128);

            // Унікальний індекс на РНОКПП
            e.HasIndex(p => p.PersonalInfo.Rnokpp)
                .HasDatabaseName("ix_persons_rnokpp")
                .IsUnique();
        });

        e.OwnsOne(x => x.MilitaryDetails, md =>
        {
            md.Property(m => m.Rank)
                .HasColumnName("rank")
                .HasMaxLength(64)
                .IsRequired();

            md.Property(m => m.BZVP)
                .HasColumnName("bzvp")
                .HasMaxLength(50)
                .IsRequired();

            md.Property(m => m.Weapon)
                .HasColumnName("weapon")
                .HasMaxLength(128);

            md.Property(m => m.Callsign)
                .HasColumnName("callsign")
                .HasMaxLength(64);
        });

        // ===== Scalar Properties =====

        e.Property(x => x.StatusKindId)
            .HasColumnName("status_kind_id");

        e.Property(x => x.PositionUnitId)
            .HasColumnName("position_unit_id");

        e.Property(x => x.CreatedUtc)
            .HasColumnName("created_utc")
            .HasColumnType("timestamp with time zone");

        e.Property(x => x.ModifiedUtc)
            .HasColumnName("modified_utc")
            .HasColumnType("timestamp with time zone");

        // ===== Relationships (частина агрегату) =====

        e.HasMany(x => x.StatusHistory)
            .WithOne()
            .HasForeignKey("PersonId")
            .OnDelete(DeleteBehavior.Cascade);

        e.HasMany(x => x.PositionAssignments)
            .WithOne()
            .HasForeignKey("PersonId")
            .OnDelete(DeleteBehavior.Cascade);

        e.HasMany(x => x.PlanActions)
            .WithOne()
            .HasForeignKey("PersonId")
            .OnDelete(DeleteBehavior.Cascade);

        // ===== Ignore Domain Events (не зберігаємо в БД) =====
        e.Ignore(x => x.DomainEvents);
    }
}
