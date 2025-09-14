// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PersonPlanningConfiguration (EF Core)
// -----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PersonPlanningConfiguration : IEntityTypeConfiguration<PersonPlanning>
{
    public void Configure(EntityTypeBuilder<PersonPlanning> e)
    {
        // ===============================
        // Table & PK
        // ===============================
        e.ToTable("person_planning");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
         .HasColumnName("id");

        // ===============================
        // Columns (lower snake_case)
        // ===============================
        e.Property(x => x.PersonId)
         .HasColumnName("person_id")
         .IsRequired();

        e.Property(x => x.CurrentStatusKindId)
         .HasColumnName("current_status_kind_id");

        e.Property(x => x.CurrentStatusKindCode)
         .HasColumnName("current_status_kind_code")
         .HasMaxLength(16);

        e.Property(x => x.LastActionType)
         .HasColumnName("last_action_type")
         .HasConversion<int?>();

        e.Property(x => x.LastActionAtUtc)
         .HasColumnName("last_action_at_utc")
         .HasColumnType("timestamp with time zone");

        e.Property(x => x.OpenLocation)
         .HasColumnName("open_location")
         .HasMaxLength(256);

        e.Property(x => x.OpenGroup)
         .HasColumnName("open_group")
         .HasMaxLength(128);

        e.Property(x => x.OpenTool)
         .HasColumnName("open_tool")
         .HasMaxLength(128);

        e.Property(x => x.Author)
         .HasColumnName("author")
         .HasMaxLength(64)
         .HasDefaultValue("system");

        e.Property(x => x.ModifiedUtc)
         .HasColumnName("modified_utc")
         .HasColumnType("timestamp with time zone")
         .HasDefaultValueSql("CURRENT_TIMESTAMP")
         .IsRequired();

        // ===============================
        // Relationships (1:1 з Person)
        // ===============================
        e.HasOne(x => x.Person)
         .WithOne(p => p.PersonPlanning)                 // додайте навігацію в Person: public PersonPlanning? PersonPlanning { get; set; }
         .HasForeignKey<PersonPlanning>(x => x.PersonId)
         .OnDelete(DeleteBehavior.Cascade);

        // ===============================
        // Indexes
        // ===============================
        e.HasIndex(x => x.PersonId)
         .IsUnique()
         .HasDatabaseName("ux_person_planning_person_id");

        e.HasIndex(x => x.ModifiedUtc)
         .HasDatabaseName("ix_person_planning_modified");
    }
}
