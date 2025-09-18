//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonStatusConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public class PersonStatusConfiguration : IEntityTypeConfiguration<PersonStatus>
{
    public void Configure(EntityTypeBuilder<PersonStatus> e)
    {
        e.ToTable("person_statuses");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id");

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.Property(x => x.StatusKindId)
            .HasColumnName("status_kind_id")
            .IsRequired();

        e.Property(x => x.Sequence)
            .HasColumnName("sequence")
            .HasDefaultValue((short)0)
            .IsRequired();

        e.Property(x => x.OpenDate)
            .HasColumnName("open_date")
            .IsRequired();

        e.Property(x => x.Note)
            .HasColumnName("note")
            .HasMaxLength(512);

        e.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        e.Property(x => x.Author)
            .HasColumnName("author")
            .HasMaxLength(64)
            .HasDefaultValue("system");

        e.Property(x => x.Modified)
            .HasColumnName("modified")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // НОВЕ: джерело
        e.Property(x => x.OrderId)
            .HasColumnName("order_id");

        e.Property(x => x.SourcePlanActionId)
            .HasColumnName("source_plan_action_id");

        e.HasOne(x => x.Person)
            .WithMany(p => p.StatusHistory)
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne<StatusKind>()
            .WithMany()
            .HasForeignKey(x => x.StatusKindId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne<Order>()
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        e.HasOne<PlanAction>()
            .WithMany()
            .HasForeignKey(x => x.SourcePlanActionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        e.HasIndex(x => new { x.PersonId, x.OpenDate })
            .HasDatabaseName("ix_person_statuses_person_open");

        e.HasIndex(x => new { x.PersonId, x.OpenDate, x.Sequence })
            .IsUnique()
            .HasFilter("is_active = TRUE")
            .HasDatabaseName("ux_person_statuses_person_open_seq_active");

        e.HasIndex(x => new { x.PersonId, x.IsActive, x.OpenDate })
            .HasDatabaseName("ix_person_statuses_person_active_open");

        e.HasIndex(x => x.OrderId)
            .HasDatabaseName("ix_person_statuses_order");

        e.HasIndex(x => x.SourcePlanActionId)
            .HasDatabaseName("ix_person_statuses_source_action");
    }
}