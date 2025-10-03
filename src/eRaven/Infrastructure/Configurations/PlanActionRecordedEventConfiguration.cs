//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActionRecordedEventConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public class PlanActionRecordedEventConfiguration : IEntityTypeConfiguration<PlanActionRecordedEvent>
{
    public void Configure(EntityTypeBuilder<PlanActionRecordedEvent> e)
    {
        // ===============================
        // Таблиця та ключі
        // ===============================
        e.ToTable("plan_action_recorded_events");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");

        // ===============================
        // Основні поля
        // ===============================
        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.Property(x => x.PlanActionName)
            .HasColumnName("plan_action_name")
            .HasMaxLength(256)
            .IsRequired();

        e.Property(x => x.EffectiveAtUtc)
            .HasColumnName("effective_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // ===============================
        // Enum поля - конверсія в int
        // ===============================
        e.Property(x => x.ActionState)
            .HasColumnName("action_state")
            .HasConversion<int>()
            .IsRequired();

        e.Property(x => x.MoveType)
            .HasColumnName("move_type")
            .HasConversion<int>()
            .IsRequired();

        // ===============================
        // Додаткова інформація
        // ===============================
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

        e.Property(x => x.Order)
            .HasColumnName("order_number")
            .HasMaxLength(512);

        // ===============================
        // Snapshot (денормалізовані дані)
        // ===============================
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

        e.Property(x => x.Callsign)
            .HasColumnName("callsign")
            .HasMaxLength(128);

        e.Property(x => x.BZVP)
            .HasColumnName("bzvp")
            .HasMaxLength(128)
            .IsRequired();

        e.Property(x => x.Weapon)
            .HasColumnName("weapon")
            .HasMaxLength(128);

        e.Property(x => x.PositionName)
            .HasColumnName("position_name")
            .HasMaxLength(128);

        e.Property(x => x.StatusKindOnDate)
            .HasColumnName("status_kind_on_date")
            .HasMaxLength(64);

        e.Property(x => x.RecordedAt)
            .HasColumnName("recorded_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // ===============================
        // Індекси для швидкого пошуку
        // ===============================
        e.HasIndex(x => new { x.PersonId, x.EffectiveAtUtc })
            .HasDatabaseName("ix_plan_action_recorded_events_person_date");

        e.HasIndex(x => x.MoveType)
            .HasDatabaseName("ix_plan_action_recorded_events_move_type");

        e.HasIndex(x => x.ActionState)
            .HasDatabaseName("ix_plan_action_recorded_events_action_state");

        e.HasIndex(x => x.EffectiveAtUtc)
            .HasDatabaseName("ix_plan_action_recorded_events_effective_date");
    }
}