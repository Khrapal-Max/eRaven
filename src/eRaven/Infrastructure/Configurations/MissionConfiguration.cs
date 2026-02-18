//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// MissionConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for <see cref="Mission"/>.
/// </summary>
public sealed class MissionConfiguration : IEntityTypeConfiguration<Mission>
{
    public void Configure(EntityTypeBuilder<Mission> e)
    {
        e.ToTable("missions", t =>
        {
            // ClosedAt >= CreatedAt (якщо ClosedAt задано)
            t.HasCheckConstraint(
                "ck_missions_dates",
                "\"closed_at\" IS NULL OR \"closed_at\" >= \"created_at\"");
        });

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        e.Property(x => x.PositionArea)
            .HasColumnName("position_area")
            .HasMaxLength(140)
            .IsRequired();

        // Domain: string? => optional (без forced default "")
        e.Property(x => x.NamePoint)
            .HasColumnName("name_point")
            .HasMaxLength(140);

        e.Property(x => x.TypeDrone)
            .HasColumnName("type_drone")
            .HasMaxLength(512);

        e.Property(x => x.Target)
            .HasColumnName("target")
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
            .HasColumnType("date");

        // Indexes
        e.HasIndex(x => x.PositionArea)
            .HasDatabaseName("ix_missions_position_area");

        e.HasIndex(x => x.MissionMode)
            .HasDatabaseName("ix_missions_mode");

        e.HasIndex(x => new { x.PositionArea, x.CreatedAt, x.ClosedAt })
            .HasDatabaseName("ix_missions_area_created_closed");

        // Одна "активна" місія з однаковими ключовими полями
        e.HasIndex(x => new { x.PositionArea, x.NamePoint, x.MissionMode, x.Target })
            .IsUnique()
            .HasFilter("\"closed_at\" IS NULL")
            .HasDatabaseName("ux_missions_active_key");
    }
}
