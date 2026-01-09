//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RankSeed
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure;

public class RankSeed
{
    public static readonly Rank[] Defaults =
    [
        new(){ Id = Guid.NewGuid(), Title = "рекрут",                   Priority = 1,  IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "солдат",                   Priority = 2,  IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "старший солдат",           Priority = 3,  IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "молодший сержант",         Priority = 4,  IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "сержант",                  Priority = 5,  IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "старший сержант",          Priority = 6,  IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "головний сержант",         Priority = 7,  IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "штаб-сержант",             Priority = 8,  IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "майстер-сержант",          Priority = 9,  IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "старший майстер-сержант",  Priority = 10, IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "головний майстер-сержант", Priority = 11, IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "молодший лейтенант",       Priority = 12, IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "лейтенант",                Priority = 13, IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "старший лейтенант",        Priority = 14, IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "капітан",                  Priority = 15, IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "майор",                    Priority = 16, IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "підполковник",             Priority = 17, IsActived = true},
        new(){ Id = Guid.NewGuid(), Title = "полковник",                Priority = 18, IsActived = true}
    ];

    public static async Task EnsureSeededAsync(AppDbContext db, CancellationToken ct)
    {
        // Якщо таблиця порожня — просто вставляємо дефолти
        if (!await db.Ranks.AsNoTracking().AnyAsync(ct))
        {
            await db.Ranks.AddRangeAsync(Defaults, ct);
            await db.SaveChangesAsync(ct);
            return;
        }
    }
}
