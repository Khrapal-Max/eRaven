//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitionConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public class StatusTransitionConfiguration : IEntityTypeConfiguration<StatusTransition>
{
    public void Configure(EntityTypeBuilder<StatusTransition> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("status_transitions");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
         .HasColumnName("id");

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

        // ===============================
        // Constraints & Indexes
        // ===============================
        e.HasIndex(x => new { x.FromStatusKindId, x.ToStatusKindId })
         .HasDatabaseName("ix_status_transitions_from_to")
         .IsUnique();

        e.ToTable(t => t.HasCheckConstraint(
            "ck_status_transitions_from_ne_to",
            "from_status_kind_id <> to_status_kind_id"
        ));

        // ===============================
        // Seed
        // ===============================
        e.HasData(Seed.GetStatus());
    }
}