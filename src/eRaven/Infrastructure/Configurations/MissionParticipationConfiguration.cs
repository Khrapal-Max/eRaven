//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// MissionParticipationConfiguration
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

/// <summary>
/// EF-конфігурація інтервалів участі в місіях.
/// </summary>
public sealed class MissionParticipationConfiguration : IEntityTypeConfiguration<MissionParticipation>
{
    public void Configure(EntityTypeBuilder<MissionParticipation> builder)
    {
        builder.ToTable("MissionParticipations", t =>
        {
            // To >= From (або To null)
            t.HasCheckConstraint(
                "CK_MissionParticipation_ToAfterFrom",
                "\"To\" IS NULL OR \"To\" >= \"From\"");

            // EndSourceDocNo задається тільки коли To задано (закрито)
            t.HasCheckConstraint(
                "CK_MissionParticipation_EndDocWhenClosed",
                "(\"To\" IS NULL AND \"EndSourceDocNo\" IS NULL) OR (\"To\" IS NOT NULL AND \"EndSourceDocNo\" IS NOT NULL)");
        });

        builder.HasKey(x => x.Id);

        // ----------------------------
        // Required + lengths
        // ----------------------------

        builder.Property(x => x.SourceDocNo)
            .IsRequired()
            .HasMaxLength(64);

        // OPTIONAL (бо на Start його нема)
        builder.Property(x => x.EndSourceDocNo)
            .HasMaxLength(64);

        builder.Property(x => x.MissionDisplaySnapshot)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.RNOKPP)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Rank)
            .HasMaxLength(64);

        builder.Property(x => x.Position)
            .HasMaxLength(256);

        builder.Property(x => x.Weapon)
            .HasMaxLength(64);

        builder.Property(x => x.Callsign)
            .HasMaxLength(64);

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.UpdatedBy).HasMaxLength(64);

        // ----------------------------
        // Indexes
        // ----------------------------

        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => new { x.DocumentId, x.GroupSequence });

        builder.HasIndex(x => new { x.PersonId, x.From });
        builder.HasIndex(x => new { x.MissionId, x.From });

        // 1 active open interval per person: To IS NULL AND NOT IsVoided
        builder.HasIndex(x => x.PersonId)
            .IsUnique()
            .HasFilter("\"To\" IS NULL AND NOT \"IsVoided\"");
    }
}
