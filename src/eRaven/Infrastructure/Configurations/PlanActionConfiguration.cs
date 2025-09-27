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

public class PlanActionConfiguration : IEntityTypeConfiguration<PlanAction>
{
    public void Configure(EntityTypeBuilder<PlanAction> e)
    {
        e.ToTable("plan_actions");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id).HasColumnName("id");

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.Property(x => x.PlanActionName)
            .HasColumnName("plan_action_name")
            .IsRequired();

        e.Property(x => x.EffectiveAtUtc)
            .HasColumnName("effective_at_utc")
            .IsRequired();

        e.Property(x => x.ToStatusKindId)
            .HasColumnName("to_status_kind_id");

        e.Property(x => x.Order)
            .HasColumnName("order_name")
            .HasMaxLength(512);

        e.Property(x => x.ActionState)
            .HasColumnName("action_state")
            .HasConversion<short>()
            .IsRequired();

        e.Property(x => x.MoveType)
            .HasColumnName("move_type")
            .HasConversion<short>()
            .IsRequired();

        e.Property(x => x.Location)
            .HasColumnName("location")
            .HasMaxLength(256)
            .IsRequired();

        e.Property(x => x.GroupName)
            .HasColumnName("group_name")
            .HasMaxLength(128);

        e.Property(x => x.CrewName)
            .HasColumnName("crew_name")
            .HasMaxLength(128);

        e.Property(x => x.Note)
            .HasColumnName("note")
            .HasMaxLength(512);

        // Snapshot
        e.Property(x => x.Rnokpp)
            .HasColumnName("rnokpp")
            .HasMaxLength(16)
            .IsRequired();

        e.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(256)
            .IsRequired();

        e.Property(x => x.RankName)
            .HasColumnName("rank_name")
            .HasMaxLength(64);

        e.Property(x => x.PositionName)
            .HasColumnName("position_name")
            .HasMaxLength(128);

        e.Property(x => x.BZVP)
            .HasColumnName("bzvp")
            .HasMaxLength(128);

        e.Property(x => x.Weapon)
            .HasColumnName("weapon")
            .HasMaxLength(128);

        e.Property(x => x.Callsign)
            .HasColumnName("callsign")
            .HasMaxLength(128);

        e.Property(x => x.StatusKindOnDate)
            .HasColumnName("status_kind_on_date")
            .HasMaxLength(64);

        e.HasOne(x => x.Person)
            .WithMany(p => p.PlanActions)
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(x => new { x.PersonId, x.EffectiveAtUtc })
            .HasDatabaseName("ix_plan_actions_person_date");

        e.HasIndex(x => x.MoveType)
            .HasDatabaseName("ix_plan_actions_move_type");
    }
}