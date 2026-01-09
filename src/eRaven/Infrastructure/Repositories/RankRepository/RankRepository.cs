//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RankRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.RankRepository;

public class RankRepository(IDbContextFactory<AppDbContext> dbFactory) : IRankRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <summary>
    /// Повертає список усіх звань, відсортованих за пріоритетом.
    /// </summary>
    /// <param name="ct">Токен скасування асинхронної операції.</param>
    /// <returns>Асинхронно повертає колекцію звань.</returns>
    public async Task<IEnumerable<Rank>> GetAllRanksAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Ranks
            .AsNoTracking()
            .OrderBy(x => x.Priority)
            .Where(x => x.IsActived)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Додає нове звання до системи.
    /// </summary>
    /// <param name="rank">Обʼєкт звання <see cref="Rank"/>, який необхідно додати.</param>
    /// <param name="ct">Токен скасування асинхронної операції.</param>
    /// <returns>Завдання, що представляє асинхронну операцію додавання.</returns>
    public async Task AddRankAsync(Rank rank, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Нормалізація (за потреби)
        rank.Title = rank.Title.Trim();

        // Мінімальний захист від некоректного пріоритету
        if (rank.Priority < 1)
            rank.Priority = 1;

        // 1) Зсунути всі записи з Priority >= нового
        await db.Ranks
            .Where(x => x.Priority >= rank.Priority)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Priority, x => x.Priority + 1), ct);

        // 2) Додати новий запис у "вільне місце"
        await db.Ranks.AddAsync(rank, ct);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
    }

    /// <summary>
    /// Деактивує звання за вказаним ідентифікатором.
    /// </summary>
    /// <param name="id">Ідентифікатор звання.</param>
    /// <param name="ct">Токен скасування асинхронної операції.</param>
    /// <returns>Завдання, що представляє асинхронну операцію деактивації.</returns>
    /// <exception cref="KeyNotFoundException">Виникає, якщо звання з указаним ідентифікатором не знайдено.</exception>
    public async Task DeActivatedRankAsync(Guid id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var rank = await db.Ranks.FirstOrDefaultAsync(x => x.Id == id, ct)
           ?? throw new KeyNotFoundException($"Rank '{id}' not found.");

        rank.IsActived = false;

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Перевіряє, чи існує активне звання з указаною назвою.
    /// </summary>
    /// <param name="title">Назва звання для перевірки.</param>
    /// <param name="ct">Токен скасування асинхронної операції.</param>
    /// <returns> <see langword="true"/>, якщо активне звання з такою назвою існує, інакше — <see langword="false"/>.</returns>
    public async Task<bool> ActiveTitleExistsAsync(string title, CancellationToken ct)
    {
        var normalized = title.Trim();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Ranks.AsNoTracking()
            .AnyAsync(x => x.Title == normalized && x.IsActived, ct);
    }
}
