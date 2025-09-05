//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitionConfiguration
//-----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class StatusKindConfiguration : IEntityTypeConfiguration<StatusKindConfiguration>
{
    public void Configure(EntityTypeBuilder<StatusKindConfiguration> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("status_transitions");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
         .HasColumnName("id")
         .ValueGeneratedOnAdd();

        // ===============================
        // Columns (lower snake_case)
        // ===============================
        e.Property(x => x.FromStatusKindId)
         .HasColumnName("from_status_kind_id")
         .IsRequired();

        e.Property(x => x.ToStatusKindId)
         .HasColumnName("to_status_kind_id")
         .IsRequired();

        // ===============================
        // Indexes
        // ===============================
        e.HasIndex(x => new { x.FromStatusKindId, x.ToStatusKindId })
         .HasDatabaseName("ux_status_transitions_from_to")
         .IsUnique();

        e.HasIndex(x => x.FromStatusKindId)
         .HasDatabaseName("ix_status_transitions_from");

        e.HasIndex(x => x.ToStatusKindId)
         .HasDatabaseName("ix_status_transitions_to");

        // ===============================
        // Relationships
        // ===============================
        e.HasOne(x => x.FromStatusKind)
         .WithMany()
         .HasForeignKey(x => x.FromStatusKindId)
         .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.ToStatusKind)
         .WithMany()
         .HasForeignKey(x => x.ToStatusKindId)
         .OnDelete(DeleteBehavior.Restrict);

        e.ToTable(t => t.HasCheckConstraint(
            "ck_status_transitions_from_ne_to",
            "\"from_status_kind_id\" <> \"to_status_kind_id\""
        ));

        // ===============================
        // Seed
        // ===============================
        e.HasData(Seed.GetStatus());
    }
}