/*//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusKindConfiguration
//-----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UI.Blazor.Domain.Models;

namespace UI.Blazor.Infrastructure.Configurations;

public sealed class StatusKindConfiguration : IEntityTypeConfiguration<StatusKind>
{
    public void Configure(EntityTypeBuilder<StatusKind> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("status_kinds");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
         .HasColumnName("id");

        // ===============================
        // Columns (lower snake_case)
        // ===============================
        e.Property(x => x.Name)
         .HasColumnName("name")
         .IsRequired()
         .HasMaxLength(128);

        e.Property(x => x.Code)
         .HasColumnName("code")
         .IsRequired()
         .HasMaxLength(16);

        e.Property(x => x.Order)
         .HasColumnName("order")
         .HasDefaultValue(0);

        e.Property(x => x.IsActive)
         .HasColumnName("is_active")
         .HasDefaultValue(true);

        e.Property(x => x.Author)
         .HasColumnName("author")
         .HasDefaultValue("system");

        e.Property(x => x.Modified)
         .HasColumnName("modified")
         .IsRequired()
         .HasDefaultValueSql("timezone('utc', now())");

        // ===============================
        // Indexes
        // ===============================
        e.HasIndex(x => x.Name)
         .HasDatabaseName("ix_status_kinds_name")
         .IsUnique();

        e.HasIndex(x => x.Code)
         .HasDatabaseName("ix_status_kinds_code");

        // ===============================
        // Seed
        // ===============================
        e.HasData(Seed.AllStatusKind);
    }
}*/