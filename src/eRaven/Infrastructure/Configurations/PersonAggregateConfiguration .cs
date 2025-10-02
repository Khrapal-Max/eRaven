//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public class PersonAggregateConfiguration : IEntityTypeConfiguration<PersonAggregate>
{
    public void Configure(EntityTypeBuilder<PersonAggregate> e)
    {
        e.ToTable("persons");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");

        // Value Objects
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

        // Current State
        e.Property(x => x.CurrentStatusKindId)
            .HasColumnName("current_status_kind_id");

        e.Property(x => x.CurrentPositionUnitId)
            .HasColumnName("current_position_unit_id");

        e.Property(x => x.CreatedUtc)
            .HasColumnName("created_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        e.Property(x => x.ModifiedUtc)
            .HasColumnName("modified_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Events (частина агрегату)
        e.HasMany(x => x.StatusHistory)
            .WithOne()
            .HasForeignKey("PersonId")
            .OnDelete(DeleteBehavior.Cascade);

        e.HasMany(x => x.PositionHistory)
            .WithOne()
            .HasForeignKey("PersonId")
            .OnDelete(DeleteBehavior.Cascade);

        e.HasMany(x => x.PlanActions)
            .WithOne()
            .HasForeignKey("PersonId")
            .OnDelete(DeleteBehavior.Cascade);

        // Зовнішні зв'язки
        e.HasOne<StatusKind>()
            .WithMany()
            .HasForeignKey(x => x.CurrentStatusKindId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne<PositionUnit>()
            .WithMany()
            .HasForeignKey(x => x.CurrentPositionUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Індекси
        e.HasIndex(p => p.PersonalInfo.Rnokpp)
            .HasDatabaseName("ix_persons_rnokpp")
            .IsUnique();

        e.HasIndex(x => x.CurrentStatusKindId)
            .HasDatabaseName("ix_persons_current_status_kind");

        e.HasIndex(x => x.CurrentPositionUnitId)
            .HasDatabaseName("ix_persons_current_position_unit");

        // Ігнорувати Domain Events
        e.Ignore(x => x.DomainEvents);
    }
}