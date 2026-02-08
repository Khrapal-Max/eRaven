//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionPointConfig
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class MissionConfig : IEntityTypeConfiguration<Mission>
{
    public void Configure(EntityTypeBuilder<Mission> e)
    {
        e.ToTable("missions");
        e.HasKey(x => x.Id);

        e.Property(x => x.PositionArea)
            .HasColumnName("position_area")
            .HasMaxLength(140)
            .IsRequired();

        e.Property(x => x.NamePoint)
            .HasColumnName("name_point")
            .HasMaxLength(140)
            .IsRequired()
            .HasDefaultValue("");

        e.Property(x => x.TypeDrone)
            .HasColumnName("type_drone")
            .HasMaxLength(512);

        e.Property(x => x.Target)
            .HasMaxLength(200)
            .IsRequired();

        e.Property(x => x.MissionMode)
            .HasColumnName("mission_mode")
            .HasConversion<int>()
            .IsRequired();

        e.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("date")
            .IsRequired();

        e.Property(x => x.ClosedAt)
            .HasColumnName("closed_at")
            .HasColumnType("date"); // nullable

        // ClosedAt >= CreatedAt (якщо ClosedAt задано)
        e.ToTable(t => t.HasCheckConstraint(
            "ck_missions_dates",
            "\"closed_at\" IS NULL OR \"closed_at\" >= \"created_at\""));

        // індекси під "активні на дату" і фільтри
        e.HasIndex(x => x.PositionArea);
        e.HasIndex(x => x.MissionMode);
        e.HasIndex(x => new { x.PositionArea, x.CreatedAt, x.ClosedAt });

        // Одна відкрита точка з локацією, назвою, режимом, метою.
        e.HasIndex(x => new { x.PositionArea, x.NamePoint, x.MissionMode, x.Target })
             .IsUnique()
             .HasFilter("\"closed_at\" IS NULL");
    }
}