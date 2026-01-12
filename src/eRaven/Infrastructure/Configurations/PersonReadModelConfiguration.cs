//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonReadModelConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PersonReadModelConfiguration : IEntityTypeConfiguration<PersonReadModel>
{
    public void Configure(EntityTypeBuilder<PersonReadModel> e)
    {
        e.ToTable("person_read");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id).HasColumnName("id");

        e.Property(x => x.Lifecycle)
            .HasColumnName("lifecycle")
            .HasConversion<int>()
            .IsRequired();

        // ✅ Enrollment columns
        e.Property(x => x.EnrollmentKind)
            .HasColumnName("enrollment_kind")
            .HasConversion<int>()          // nullable enum -> int?
            .IsRequired(false);

        e.Property(x => x.EnrollmentReference)
            .HasColumnName("enrollment_reference")
            .HasMaxLength(256);

        e.Property(x => x.Rnokpp)
            .HasColumnName("rnokpp")
            .HasMaxLength(10)
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

        e.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(512)
            .IsRequired();

        e.Property(x => x.Rank)
            .HasColumnName("rank")
            .HasMaxLength(128);

        e.Property(x => x.PositionSort)
            .HasColumnName("position_sort");

        e.Property(x => x.Position)
            .HasColumnName("position")
            .HasMaxLength(512);

        e.Property(x => x.Bzvp)
            .HasColumnName("bzvp")
            .HasMaxLength(128);

        e.Property(x => x.Weapon)
            .HasColumnName("weapon")
            .HasMaxLength(128);

        e.Property(x => x.Callsign)
            .HasColumnName("callsign")
            .HasMaxLength(128);

        e.Property(x => x.EnrolledAt)
            .HasColumnName("enrolled_at");

        e.Property(x => x.ExcludedAt)
            .HasColumnName("excluded_at");

        e.Property(x => x.Version)
            .HasColumnName("version")
            .IsRequired();

        e.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        // ===== indexes =====
        e.HasIndex(x => x.Lifecycle)
            .HasDatabaseName("ix_person_read_lifecycle");

        e.HasIndex(x => x.Rnokpp)
            .HasDatabaseName("ux_person_read_rnokpp")
            .IsUnique();

        e.HasIndex(x => new { x.EnrolledAt, x.ExcludedAt })
            .HasDatabaseName("ix_person_read_enrolled_excluded");

        e.HasIndex(x => new { x.EnrollmentKind, x.PositionSort, x.LastName })
            .HasDatabaseName("ix_person_read_kind_possort_last");
    }
}