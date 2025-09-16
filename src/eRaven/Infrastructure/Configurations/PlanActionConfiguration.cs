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
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("plan_actions");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");

        // ===============================
        // Columns
        // ===============================
        e.Property(x => x.PlanId)
         .HasColumnName("plan_id")
         .IsRequired();

        e.Property(x => x.PersonId)
         .HasColumnName("person_id")
         .IsRequired();

        e.Property(x => x.ActionType)
         .HasColumnName("action_type")
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

        // ------- Snapshot fields -------
        e.Property(x => x.Rnokpp)
         .HasColumnName("rnokpp")
         .HasMaxLength(10)
         .IsRequired();

        e.Property(x => x.FullName)
         .HasColumnName("full_name")
         .HasMaxLength(256)
         .IsRequired();

        e.Property(x => x.RankName)
         .HasColumnName("rank_name")
         .HasMaxLength(64)
         .IsRequired();

        e.Property(x => x.PositionName)
         .HasColumnName("position_name")
         .HasMaxLength(512)
         .IsRequired();

        e.Property(x => x.BZVP)
         .HasColumnName("bzvp")
         .HasMaxLength(50)
         .IsRequired();

        e.Property(x => x.Weapon)
         .HasColumnName("weapon")
         .HasMaxLength(128);

        e.Property(x => x.Callsign)
         .HasColumnName("callsign")
         .HasMaxLength(64);

        e.Property(x => x.StatusKindOnDate)
         .HasColumnName("status_kind_on_date")
         .HasMaxLength(64)
         .IsRequired();

        // ===============================
        // Indexes & Constraints
        // ===============================
        // швидкий “останній плановий стан по особі”
        e.HasIndex(x => new { x.PersonId, x.EventAtUtc })
         .HasDatabaseName("ix_plan_actions_person_last");

        // остання дія в межах плану по особі
        e.HasIndex(x => new { x.PlanId, x.PersonId, x.EventAtUtc })
         .HasDatabaseName("ix_plan_actions_plan_person_last");

        // ===============================
        // Relationships
        // ===============================
        e.HasOne(x => x.Plan)
         .WithMany(p => p.PlanActions)
         .HasForeignKey(x => x.PlanId)
         .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Person)
         .WithMany() // якщо додаси навігацію у Person, заміни на .WithMany(p => p.PlanActions)
         .HasForeignKey(x => x.PersonId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}