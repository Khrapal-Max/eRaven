//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RankRepositoryTests -> RankRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Infrastructure.Repositories.RankRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public class RankRepositoryTests : IAsyncLifetime
{
    private SqliteTestDb _db = default!;
    private RankRepository _repo = default!;

    public async Task InitializeAsync()
    {
        _db = new SqliteTestDb();
        _repo = new RankRepository(_db.Factory);
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    private static Rank NewRank(string title, int priority, bool isActived)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Priority = priority,
            IsActived = isActived
        };

    [Fact]
    public async Task AddRankAsync_ShouldPersistRank()
    {
        // Arrange
        var rank = NewRank("Captain", priority: 10, isActived: true);

        // Act
        await _repo.AddRankAsync(rank, CancellationToken.None);

        // Assert
        using var db = _db.Factory.CreateDbContext();
        var saved = await db.Ranks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == rank.Id);

        Assert.NotNull(saved);
        Assert.Equal("Captain", saved!.Title);
        Assert.Equal(10, saved.Priority);
        Assert.True(saved.IsActived);
    }

    [Fact]
    public async Task GetAllRanksAsync_ShouldReturnRanksOrderedByPriority()
    {
        // Arrange
        var r1 = NewRank("R2", priority: 2, isActived: true);
        var r2 = NewRank("R1", priority: 1, isActived: true);
        var r3 = NewRank("R3", priority: 3, isActived: true);

        await _repo.AddRankAsync(r1, CancellationToken.None);
        await _repo.AddRankAsync(r2, CancellationToken.None);
        await _repo.AddRankAsync(r3, CancellationToken.None);

        // Act
        var result = (await _repo.GetAllRanksAsync(CancellationToken.None)).ToList();

        // Assert: рівно 3, і вони відсортовані за Priority по зростанню
        Assert.Equal(3, result.Count);

        var priorities = result.Select(x => x.Priority).ToList();
        Assert.True(priorities.SequenceEqual(priorities.OrderBy(x => x)));

        // Додатково можна перевірити, що всі титули присутні
        Assert.Contains("R1", result.Select(x => x.Title));
        Assert.Contains("R2", result.Select(x => x.Title));
        Assert.Contains("R3", result.Select(x => x.Title));
    }

    [Fact]
    public async Task DeActivatedRankAsync_ShouldSetIsActivedFalse()
    {
        // Arrange
        var rank = NewRank("Major", priority: 5, isActived: true);
        await _repo.AddRankAsync(rank, CancellationToken.None);

        // Act
        await _repo.DeActivatedRankAsync(rank.Id, CancellationToken.None);

        // Assert
        using var db = _db.Factory.CreateDbContext();
        var updated = await db.Ranks.AsNoTracking().FirstAsync(x => x.Id == rank.Id);

        Assert.False(updated.IsActived);
    }

    [Fact]
    public async Task DeActivatedRankAsync_WhenNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _repo.DeActivatedRankAsync(missingId, CancellationToken.None));
    }

    [Fact]
    public async Task ActiveTitleExistsAsync_ShouldReturnTrue_WhenActiveRankWithTitleExists()
    {
        // Arrange
        await _repo.AddRankAsync(NewRank("Lieutenant", priority: 1, isActived: true), CancellationToken.None);

        // Act
        var exists = await _repo.ActiveTitleExistsAsync("Lieutenant", CancellationToken.None);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ActiveTitleExistsAsync_ShouldReturnFalse_WhenOnlyInactiveRankWithTitleExists()
    {
        // Arrange
        await _repo.AddRankAsync(NewRank("Colonel", priority: 1, isActived: false), CancellationToken.None);

        // Act
        var exists = await _repo.ActiveTitleExistsAsync("Colonel", CancellationToken.None);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ActiveTitleExistsAsync_ShouldTrimInput()
    {
        // Arrange
        await _repo.AddRankAsync(NewRank("Sergeant", priority: 1, isActived: true), CancellationToken.None);

        // Act
        var exists = await _repo.ActiveTitleExistsAsync("  Sergeant  ", CancellationToken.None);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ActiveTitleExistsAsync_ShouldReturnTrue_WhenAnyActiveExists_EvenIfInactiveAlsoExists()
    {
        // Arrange
        await _repo.AddRankAsync(NewRank("Private", priority: 1, isActived: false), CancellationToken.None);
        await _repo.AddRankAsync(NewRank("Private", priority: 2, isActived: true), CancellationToken.None);

        // Act
        var exists = await _repo.ActiveTitleExistsAsync("Private", CancellationToken.None);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task AddRankAsync_WhenInsertAtPriority_ShouldShiftExistingPriorities()
    {
        // Arrange
        var repo = _repo; // якщо в тебе як раніше в тест-класі
        var ct = CancellationToken.None;

        // створюємо ранги з пріоритетами 1..7
        for (var p = 1; p <= 7; p++)
        {
            await repo.AddRankAsync(new Rank
            {
                Id = Guid.NewGuid(),
                Title = $"R{p}",
                Priority = p,
                IsActived = true
            }, ct);
        }

        // Act: вставка на 5
        var inserted = new Rank
        {
            Id = Guid.NewGuid(),
            Title = "Inserted",
            Priority = 5,
            IsActived = true
        };

        await repo.AddRankAsync(inserted, ct);

        // Assert
        var all = (await repo.GetAllRanksAsync(ct)).ToList();

        // має стати 8 записів
        Assert.Equal(8, all.Count);

        // перевіряємо, що Inserted реально на Priority=5
        var insertedFromDb = all.Single(x => x.Title == "Inserted");
        Assert.Equal(5, insertedFromDb.Priority);

        // перевіряємо весь порядок по title (бо пріоритети змістились)
        // очікування: R1,R2,R3,R4,Inserted,R5,R6,R7
        Assert.Equal(
            [ "R1", "R2", "R3", "R4", "Inserted", "R5", "R6", "R7" ],
            [.. all.Select(x => x.Title)]);

        // точкова перевірка зсуву:
        // старий R5 був 5 -> має стати 6
        Assert.Equal(6, all.Single(x => x.Title == "R5").Priority);
        Assert.Equal(7, all.Single(x => x.Title == "R6").Priority);
        Assert.Equal(8, all.Single(x => x.Title == "R7").Priority);
    }
}
