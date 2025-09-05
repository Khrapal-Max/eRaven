//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanParticipantSnapshotConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PlanParticipantSnapshotConfiguration : IEntityTypeConfiguration<PlanParticipantSnapshot>
{
    public void Configure(EntityTypeBuilder<PlanParticipantSnapshot> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("plan_participant_snapshots");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
         .HasColumnName("id");

        // ===============================
        // Columns (lower snake_case)
        // ===============================
        e.Property(x => x.PlanId)
         .HasColumnName("plan_id")
         .IsRequired();

        e.Property(x => x.PersonId)
         .HasColumnName("person_id")
         .IsRequired();

        e.Property(x => x.FullName)
         .HasColumnName("full_name")
         .HasMaxLength(256)
         .IsRequired();

        e.Property(x => x.Rank)
         .HasColumnName("rank")
         .HasMaxLength(64);

        e.Property(x => x.PositionSnapshot)
         .HasColumnName("position_snapshot")
         .HasMaxLength(256);

        e.Property(x => x.Weapon)
         .HasColumnName("weapon")
         .HasMaxLength(128);

        e.Property(x => x.Callsign)
         .HasColumnName("callsign")
         .HasMaxLength(64);

        e.Property(x => x.StatusKindId)
         .HasColumnName("status_kind_id");

        e.Property(x => x.StatusKindCode)
         .HasColumnName("status_kind_code")
         .HasMaxLength(16);

        e.Property(x => x.Author)
         .HasColumnName("author")
         .HasMaxLength(64)
         .HasDefaultValue("system");

        e.Property(x => x.RecordedUtc)
         .HasColumnName("recorded_utc")
         .HasColumnType("timestamp with time zone")
         .HasDefaultValueSql("now()")
         .IsRequired();

        // ===============================
        // Relationships
        // ===============================
        // Snapshot -> Plan : many-to-1 (каскадне видалення разом із планом)
        e.HasOne<Plan>()
         .WithMany(p => p.Participants)
         .HasForeignKey(x => x.PlanId)
         .OnDelete(DeleteBehavior.Cascade);

        // ===============================
        // Constraints & Indexes
        // ===============================
        e.HasIndex(x => x.PlanId)
         .HasDatabaseName("ix_pps_plan_id");

        e.HasIndex(x => x.PersonId)
         .HasDatabaseName("ix_pps_person_id");

        e.HasIndex(x => new { x.PlanId, x.PersonId })
         .HasDatabaseName("ix_pps_plan_person");

        e.HasIndex(x => x.RecordedUtc)
         .HasDatabaseName("ix_pps_recorded_utc");

        e.ToTable(t => t.HasCheckConstraint(
            "ck_pps_full_name_not_blank",
            "char_length(trim(full_name)) > 0"
        ));
    }
}
