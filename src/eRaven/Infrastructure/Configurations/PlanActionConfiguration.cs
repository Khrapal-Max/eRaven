//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActionConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PlanActionConfiguration : IEntityTypeConfiguration<PlanAction>
{
    public void Configure(EntityTypeBuilder<PlanAction> e)
    {
        e.ToTable("plan_actions");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id).HasColumnName("id");

        e.Property(x => x.PlanId)
            .HasColumnName("plan_id")
            .IsRequired();

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.Property(x => x.EffectiveAtUtc)
            .HasColumnName("effective_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        e.Property(x => x.ToStatusKindId)
            .HasColumnName("to_status_kind_id")
            .IsRequired();

        e.Property(x => x.TripId)
            .HasColumnName("trip_id");

        e.Property(x => x.State)
            .HasColumnName("state")
            .HasConversion<short>() // з enum -> smallint
            .IsRequired();

        // Snapshot
        e.Property(x => x.Rnokpp)
            .HasColumnName("rnokpp")
            .HasMaxLength(10)
            .IsRequired();

        e.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(384)
            .IsRequired();

        e.Property(x => x.RankName)
            .HasColumnName("rank_name")
            .HasMaxLength(64)
            .IsRequired();

        e.Property(x => x.PositionName)
            .HasColumnName("position_name")
            .HasMaxLength(256)
            .IsRequired();

        e.Property(x => x.BZVP)
            .HasColumnName("bzvp")
            .HasMaxLength(50)
            .IsRequired();

        e.Property(x => x.Weapon)
            .HasColumnName("weapon")
            .HasMaxLength(128)
            .IsRequired();

        e.Property(x => x.Callsign)
            .HasColumnName("callsign")
            .HasMaxLength(64)
            .IsRequired();

        e.Property(x => x.StatusKindOnDate)
            .HasColumnName("status_kind_on_date")
            .HasMaxLength(128)
            .IsRequired();

        // FK
        e.HasOne<Plan>()
            .WithMany(p => p.PlanActions)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Person)
            .WithMany()
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne<StatusKind>()
            .WithMany()
            .HasForeignKey(x => x.ToStatusKindId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        e.HasIndex(x => new { x.PersonId, x.EffectiveAtUtc })
            .HasDatabaseName("ix_planactions_person_effective");

        e.HasIndex(x => new { x.PlanId, x.PersonId })
            .HasDatabaseName("ix_planactions_plan_person");

        e.HasIndex(x => x.TripId)
            .HasDatabaseName("ix_planactions_trip");

        // (опційно) Забезпечити унікальний Trip на особу
        e.HasIndex(x => new { x.PersonId, x.TripId })
            .HasDatabaseName("ux_planactions_person_trip")
            .IsUnique()
            .HasFilter("\"trip_id\" IS NOT NULL");

        // (опційно) контроль валідних значень state
        e.ToTable(t => t.HasCheckConstraint(
            "ck_planactions_state_range",
            "state in (0,1,2)"
        ));
    }
}