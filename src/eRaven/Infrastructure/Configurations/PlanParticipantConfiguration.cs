//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanParticipantConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PlanParticipantConfiguration : IEntityTypeConfiguration<PlanParticipant>
{
    public void Configure(EntityTypeBuilder<PlanParticipant> e)
    {
        e.ToTable("plan_participants");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id");

        e.Property(x => x.PlanId)
         .HasColumnName("plan_id")
         .IsRequired();

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.HasOne(x => x.Plan)
         .WithMany(p => p.Participants)
         .HasForeignKey(x => x.PlanId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Person)
         .WithMany()
         .HasForeignKey(x => x.PersonId)
         .OnDelete(DeleteBehavior.Restrict);

        // Снапшот атрибутів
        e.Property(x => x.FullName)
         .HasColumnName("full_name")
         .HasMaxLength(256)
         .IsRequired();

        e.Property(x => x.RankName)
         .HasColumnName("rank_name")
         .HasMaxLength(128)
         .IsRequired();

        e.Property(x => x.PositionName)
         .HasColumnName("position_name")
         .HasMaxLength(128)
         .IsRequired();

        e.Property(x => x.UnitName)
         .HasColumnName("unit_name")
         .HasMaxLength(128)
         .IsRequired();

        // Audit
        e.Property(x => x.Author)
         .HasColumnName("author")
         .HasMaxLength(64)
         .HasDefaultValue("system");

        e.Property(x => x.RecordedUtc)
         .HasColumnName("recorded_utc")
         .HasColumnType("timestamp with time zone")
         .HasDefaultValueSql("CURRENT_TIMESTAMP")
         .IsRequired();

        // Унікальність — одна людина у плані тільки раз
        e.HasIndex(x => new { x.PlanId, x.PersonId })
         .IsUnique()
         .HasDatabaseName("ux_plan_participants_plan_person");
    }
}
