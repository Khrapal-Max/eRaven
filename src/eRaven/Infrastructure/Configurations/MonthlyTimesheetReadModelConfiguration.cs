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

        // composite key
        e.HasKey(x => new { x.PersonId, x.Year, x.Month });

        e.Property(x => x.Year)
            .IsRequired();

        e.Property(x => x.Month)
            .IsRequired();

        e.Property(x => x.Version)
            .IsRequired();

        e.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        // typed collection -> separate table (NO JSON)
        e.OwnsMany(x => x.Days, d =>
        {
            d.ToTable("monthly_timesheet_days");

            d.WithOwner()
            .HasForeignKey("PersonId", "Year", "Month");

            // composite key for day cells
            d.HasKey("PersonId", "Year", "Month", nameof(MonthlyTimesheetDay.Day), nameof(MonthlyTimesheetDay.Lane));

            d.Property(x => x.Day)
                .HasColumnName("day")
                .IsRequired();

            d.Property(x => x.Lane)
                .HasConversion<int>()
                .IsRequired();

            d.Property(x => x.Code)
                .HasMaxLength(32)
                .IsRequired();

            d.Property(x => x.EntryId)
                .IsRequired(false);

            d.HasIndex("PersonId", "Year", "Month");
        });

        e.Navigation(x => x.Days).AutoInclude(false);
    }
}