//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionActionPersonConfig
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eRaven.Infrastructure.Configurations;

public sealed class MissionActionPersonConfig : IEntityTypeConfiguration<MissionActionPerson>
{
    public void Configure(EntityTypeBuilder<MissionActionPerson> b)
    {
        b.ToTable("mission_action_people");

        // 1 людина не може бути двічі у тій самій дії
        b.HasKey(x => new { x.ActionId, x.PersonId });

        b.Property(x => x.ActionId)
            .HasColumnName("action_id")
            .IsRequired();

        b.Property(x => x.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        b.HasIndex(x => x.PersonId);

        // при видаленні action — каскадно прибираємо зв’язки (зручно)
        b.HasOne<MissionAction>()
            .WithMany()
            .HasForeignKey(x => x.ActionId)
            .OnDelete(DeleteBehavior.Cascade);

        // PersonId: FK додавай лише якщо Person зберігається в цій же БД і є таблиця persons
        //b.HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
    }
}