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
    public void Configure(EntityTypeBuilder<Mission> b)
    {
        b.ToTable("missions");
        b.HasKey(x => x.Id);

        b.Property(x => x.PositionArea)
            .HasMaxLength(140)
            .IsRequired();

        b.Property(x => x.NamePoint)
            .HasMaxLength(140)
            .IsRequired()
            .HasDefaultValue("");

        b.Property(x => x.TypeDrone)
            .HasMaxLength(512);

        b.Property(x => x.Target)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.MissionMode)
            .HasConversion<int>()
            .IsRequired();

        b.Property(x => x.CreatedAt)
            .HasColumnType("date")
            .IsRequired();

        b.Property(x => x.ClosedAt)
            .HasColumnType("date"); // nullable

        // ClosedAt >= CreatedAt (якщо ClosedAt задано)
        b.ToTable(t => t.HasCheckConstraint(
            "ck_missions_dates",
            "\"ClosedAt\" IS NULL OR \"ClosedAt\" >= \"CreatedAt\""));

        // UI-only
        b.Ignore(x => x.DisplayMission);

        // індекси під "активні на дату" і фільтри
        b.HasIndex(x => x.PositionArea);
        b.HasIndex(x => x.MissionMode);
        b.HasIndex(x => new { x.PositionArea, x.CreatedAt, x.ClosedAt });

        // Одна відкрита точка з локацією, назвою, режимом, метою.
        b.HasIndex(x => new { x.PositionArea, x.NamePoint, x.MissionMode, x.Target })
             .IsUnique()
             .HasFilter("\"ClosedAt\" IS NULL");
    }
}