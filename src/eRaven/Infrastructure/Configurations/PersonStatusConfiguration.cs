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

        e.Property(x => x.FromDate)
         .HasColumnName("from_utc")        // було: from_date
         .IsRequired();

        e.Property(x => x.ToDate)
         .HasColumnName("to_utc");          // було: to_date

        e.Property(x => x.Note)
         .HasColumnName("note")
         .HasMaxLength(512);

        e.Property(x => x.IsActive)
         .HasColumnName("is_active")
         .HasDefaultValue(true);

        e.Property(x => x.Author)
         .HasColumnName("author")
         .HasDefaultValue("system");

        e.Property(x => x.Modified)
         .HasColumnName("modified")
         .IsRequired()
         .HasDefaultValueSql("CURRENT_TIMESTAMP"); // було timezone('utc', now())

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
        // Constraints & Indexes
        // ===============================

        // Активний статус тільки один на особу (to_date IS NULL)
        e.HasIndex(x => x.PersonId)
         .HasFilter("\"to_utc\" IS NULL")   // було: "to_date" IS NULL
         .HasDatabaseName("ix_person_statuses_active_unique_per_person")
         .IsUnique();

        // Базова валідація інтервалу
        e.ToTable(t => t.HasCheckConstraint(
             "ck_person_status_dates",
             "(\"to_utc\" IS NULL) OR (\"to_utc\" > \"from_utc\")"  // було: to_date/from_date
         ));

        // Індекси для пошуку по періодах
        e.HasIndex(x => new { x.PersonId, x.FromDate })
         .HasDatabaseName("ix_person_statuses_person_from");

        e.HasIndex(x => new { x.PersonId, x.ToDate })
         .HasDatabaseName("ix_person_statuses_person_to");
    }
}
