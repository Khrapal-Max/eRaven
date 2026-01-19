//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MonthlyTimesheetReadModelConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class MonthlyTimesheetReadModelConfiguration : IEntityTypeConfiguration<MonthlyTimesheetReadModel>
{
    public void Configure(EntityTypeBuilder<MonthlyTimesheetReadModel> e)
    {
        e.ToTable("monthly_timesheets");

        e.HasKey(x => new { x.PersonId, x.Year, x.Month });

        e.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        e.Property(x => x.Year)
            .HasColumnName("year")
            .IsRequired();

        e.Property(x => x.Month)
            .HasColumnName("month")
            .IsRequired();

        e.Property(x => x.DaysJson)
            .HasColumnName("days_json")
            .IsRequired();

        e.Property(x => x.Version)
            .HasColumnName("version")
            .IsRequired();

        e.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        e.HasIndex(x => new { x.Year, x.Month })
            .HasDatabaseName("ix_monthly_timesheet_year_month");
    }
}
