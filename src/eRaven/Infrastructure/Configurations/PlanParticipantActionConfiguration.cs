//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanParticipantActionConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PlanParticipantActionConfiguration : IEntityTypeConfiguration<PlanParticipantAction>
{
    public void Configure(EntityTypeBuilder<PlanParticipantAction> e)
    {
        e.ToTable("plan_participant_actions");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
         .HasColumnName("id");

        e.Property(x => x.PlanParticipantId)
         .HasColumnName("plan_participant_id")
         .IsRequired();

        e.Property(x => x.PlanId)
         .HasColumnName("plan_id")
         .IsRequired();

        e.Property(x => x.PersonId)
         .HasColumnName("person_id")
         .IsRequired();

        e.HasOne(x => x.PlanParticipant)
         .WithMany(p => p.Actions)
         .HasForeignKey(x => x.PlanParticipantId)
         .OnDelete(DeleteBehavior.Cascade);

        e.Property(x => x.ActionType)
         .HasColumnName("action_type")
         .HasConversion<int>()
         .IsRequired();

        e.Property(x => x.EventAtUtc)
         .HasColumnName("event_at_utc")
         .HasColumnType("timestamp with time zone")
         .IsRequired();

        e.Property(x => x.Location)
         .HasColumnName("location")
         .HasMaxLength(128)
         .IsRequired();

        e.Property(x => x.GroupName)
         .HasColumnName("group_name")
         .HasMaxLength(128)
         .IsRequired();

        e.Property(x => x.CrewName)
         .HasColumnName("crew_name")
         .HasMaxLength(128)
         .IsRequired();

        e.Property(x => x.Note)
         .HasColumnName("note")
         .HasMaxLength(512);

        e.Property(x => x.Author)
         .HasColumnName("author")
         .HasMaxLength(64)
         .HasDefaultValue("system");

        e.Property(x => x.RecordedUtc)
         .HasColumnName("recorded_utc")
         .HasColumnType("timestamp with time zone")
         .HasDefaultValueSql("CURRENT_TIMESTAMP")
         .IsRequired();

        // Унікальність: один Dispatch і один Return для учасника (базовий сценарій)
        e.HasIndex(x => new { x.PlanParticipantId, x.ActionType })
         .IsUnique()
         .HasDatabaseName("ux_actions_participant_type");

        e.HasIndex(x => x.EventAtUtc).HasDatabaseName("ix_actions_event_at");
    }
}
