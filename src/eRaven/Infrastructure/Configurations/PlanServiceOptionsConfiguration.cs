//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanServiceOptionsConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class PlanServiceOptionsConfiguration : IEntityTypeConfiguration<PlanServiceOptions>
{
    public void Configure(EntityTypeBuilder<PlanServiceOptions> e)
    {
        // Table & PK
        e.ToTable("plan_service_options");
        e.HasKey(x => x.Id);

        // Columns (lower snake_case)
        e.Property(x => x.Id)
         .HasColumnName("id");

        e.Property(x => x.DispatchStatusKindId)
         .HasColumnName("dispatch_status_kind_id");

        e.Property(x => x.ReturnStatusKindId)
         .HasColumnName("return_status_kind_id");

        e.Property(x => x.Author)
         .HasColumnName("author")
         .HasMaxLength(64)
         .HasDefaultValue("system");

        e.Property(x => x.ModifiedUtc)
         .HasColumnName("modified_utc")
         .HasDefaultValueSql("CURRENT_TIMESTAMP")
         .IsRequired();

        // Relationships
        e.HasOne(x => x.DispatchStatusKind)
         .WithMany()
         .HasForeignKey(x => x.DispatchStatusKindId)
         .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.ReturnStatusKind)
         .WithMany()
         .HasForeignKey(x => x.ReturnStatusKindId)
         .OnDelete(DeleteBehavior.Restrict);

        // Indexes (опційно, для швидких джойнів)
        e.HasIndex(x => x.DispatchStatusKindId)
         .HasDatabaseName("ix_plan_opts_dispatch_kind");

        e.HasIndex(x => x.ReturnStatusKindId)
         .HasDatabaseName("ix_plan_opts_return_kind");

        // Constraints
        e.ToTable(t => t.HasCheckConstraint(
            "ck_plan_opts_dispatch_ne_return",
            "(dispatch_status_kind_id IS NULL) OR (return_status_kind_id IS NULL) OR (dispatch_status_kind_id <> return_status_kind_id)"
        ));

        e.HasData(Seed.Setoptions);
    }
}
