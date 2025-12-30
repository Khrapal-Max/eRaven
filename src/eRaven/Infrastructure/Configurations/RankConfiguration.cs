//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RankConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public class RankConfiguration : IEntityTypeConfiguration<Rank>
{
    public void Configure(EntityTypeBuilder<Rank> e)
    {
        // ===============================
        // Table & Keys
        // ===============================
        e.ToTable("ranks");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
         .HasColumnName("id");

        // ===============================
        // Columns (lower snake_case)
        // ===============================
        e.Property(x => x.Title)
         .HasColumnName("title")
         .HasMaxLength(512)
         .IsRequired();

        e.Property(x => x.Priority)
         .HasColumnName("priority")
         .HasColumnType("int")
         .IsRequired();        

        e.Property(x => x.IsActived)
         .HasColumnName("is_active")
         .HasDefaultValue(true)
         .IsRequired();

        // ===============================
        // Indexes
        // ===============================
        e.HasIndex(x => x.Title)
        .IsUnique();
    }
}
