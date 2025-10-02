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
        e.ToTable("status_transitions");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");

        e.Property(x => x.FromStatusKindId)
            .HasColumnName("from_status_kind_id")
            .IsRequired();

        e.Property(x => x.ToStatusKindId)
            .HasColumnName("to_status_kind_id")
            .IsRequired();

        // Зв'язки
        e.HasOne<StatusKind>()
            .WithMany()
            .HasForeignKey(x => x.FromStatusKindId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne<StatusKind>()
            .WithMany()
            .HasForeignKey(x => x.ToStatusKindId)
            .OnDelete(DeleteBehavior.Restrict);

        // Індекси та обмеження
        e.HasIndex(x => new { x.FromStatusKindId, x.ToStatusKindId })
            .HasDatabaseName("ix_status_transitions_from_to").IsUnique();

        e.HasIndex(x => x.FromStatusKindId)
            .HasDatabaseName("ix_status_transitions_from");

        e.ToTable(t => t.HasCheckConstraint(
            "ck_status_transitions_from_ne_to",
            "from_status_kind_id <> to_status_kind_id"
        ));

        // Початкові дані
        e.HasData(Seed.GetStatusTransitions());
    }
}