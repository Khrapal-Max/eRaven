//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonStatusConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PersonStatusConfiguration : IEntityTypeConfiguration<PersonStatus>
{
    public void Configure(EntityTypeBuilder<PersonStatus> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("person_statuses");
        e.HasKey(x => x.Id);

        // ===============================
        // Columns (lower snake_case)
        // ===============================
        e.Property(x => x.Id)
            .HasColumnName("id");

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.Property(x => x.StatusKindId)
            .HasColumnName("status_kind_id")
            .IsRequired();

        e.Property(x => x.OpenDate)
            .HasColumnName("open_date")
            .IsRequired();

        e.Property(x => x.Note)
            .HasColumnName("note")
            .HasMaxLength(512);

        e.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        e.Property(x => x.Author)
            .HasColumnName("author")
            .HasMaxLength(64)
            .HasDefaultValue("system");

        e.Property(x => x.Modified)
            .HasColumnName("modified")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // ===============================
        // Relationships
        // ===============================
        e.HasOne(x => x.Person)
            .WithMany(p => p.StatusHistory)
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.StatusKind)
            .WithMany()
            .HasForeignKey(x => x.StatusKindId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===============================
        // Indexes
        // ===============================
        // Основний індекс для історії (перегляди/звітність)
        e.HasIndex(x => new { x.PersonId, x.OpenDate })
            .HasDatabaseName("ix_person_statuses_person_open");

        // Унікальний (partial) — лише для валідних записів:
        // гарантує, що в один і той самий момент часу може існувати
        // не більше одного "активного" запису для особи.
        // (PostgreSQL підтримує фільтр; у SQLite під час тестів фільтр ігнорується)
        e.HasIndex(x => new { x.PersonId, x.OpenDate })
            .IsUnique()
            .HasFilter("is_active = TRUE")
            .HasDatabaseName("ux_person_statuses_person_open_active");

        // Додатковий індекс для швидкого пошуку "поточних" (валідних) з сортуванням по даті
        e.HasIndex(x => new { x.PersonId, x.IsActive, x.OpenDate })
            .HasDatabaseName("ix_person_statuses_person_active_open");
    }
}
