//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntryQueryRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

/// <summary>
/// Тести для <see cref="TimesheetEntryQueryRepository"/>.
///
/// <para>
/// Фіксуємо контракт read-only запитів по <see cref="TimesheetEntry"/>:
/// <list type="bullet">
/// <item><description>Інтервали зберігаються як <c>[From..To)</c>, <c>To</c> — <b>exclusive</b>.</description></item>
/// <item><description>Soft-deleted (<c>IsDeleted=true</c>) ігноруються в усіх запитах.</description></item>
/// <item><description>GetActiveEntryOnDateAsync / GetNextEntryAfterDateAsync працюють в межах епізоду, який покриває дату.</description></item>
/// <item><description>Якщо дані епізодів пошкоджені (накладання) — методи кидають виняток (SingleOrDefault).</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class TimesheetEntryQueryRepositoryTests
{
    //======================================================================
    // GetEntriesForPersonsAsync
    //======================================================================

    /// <summary>
    /// Якщо personIds порожній або діапазон невалідний — повертає порожній список.
    /// </summary>
    [Fact]
    public async Task GetEntriesForPersonsAsync_ReturnsEmpty_WhenInputEmptyOrRangeInvalid()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryQueryRepository(testDb.Factory);

        var from = new DateOnly(2026, 02, 10);
        var toExclusive = new DateOnly(2026, 02, 10);

        var emptyPersons = await repo.GetEntriesForPersonsAsync([], from, new DateOnly(2026, 02, 20));
        Assert.Empty(emptyPersons);

        var invalidRange = await repo.GetEntriesForPersonsAsync([Guid.NewGuid()], from, toExclusive);
        Assert.Empty(invalidRange);
    }

    /// <summary>
    /// Повертає лише записи, що перетинають діапазон <c>[from..toExclusive)</c> (half-open),
    /// впорядковано за PersonId, From. Soft-deleted ігноруються.
    /// </summary>
    [Fact]
    public async Task GetEntriesForPersonsAsync_ReturnsOverlaps_Ordered_AndIgnoresDeleted()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryQueryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var codeId = await SeedCodeAsync(testDb, "T", now);

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var ts1 = await SeedEpisodeAsync(testDb, p1, new DateOnly(2026, 02, 01), null, "seed", now);
        var ts2 = await SeedEpisodeAsync(testDb, p2, new DateOnly(2026, 02, 01), null, "seed", now);

        // p1
        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = ts1,
            PersonId = p1,
            TimesheetCodeDefinitionId = codeId,
            From = new DateOnly(2026, 01, 25),
            To = new DateOnly(2026, 02, 02), // does NOT overlap when from == To (exclusive)
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false
        });

        var p1e1 = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = ts1,
            PersonId = p1,
            TimesheetCodeDefinitionId = codeId,
            From = new DateOnly(2026, 02, 01),
            To = new DateOnly(2026, 02, 05),
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false
        };
        await SeedEntryAsync(testDb, p1e1);

        var p1e2 = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = ts1,
            PersonId = p1,
            TimesheetCodeDefinitionId = codeId,
            From = new DateOnly(2026, 02, 05),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false
        };
        await SeedEntryAsync(testDb, p1e2);

        // deleted overlap for p1
        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = ts1,
            PersonId = p1,
            TimesheetCodeDefinitionId = codeId,
            From = new DateOnly(2026, 02, 03),
            To = new DateOnly(2026, 02, 04),
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = true,
            DeletedBy = "seed",
            DeletedAtUtc = now,
            DeleteReason = "test"
        });

        // p2
        var p2e1 = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = ts2,
            PersonId = p2,
            TimesheetCodeDefinitionId = codeId,
            From = new DateOnly(2026, 02, 02),
            To = new DateOnly(2026, 02, 06),
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false
        };
        await SeedEntryAsync(testDb, p2e1);

        // Act
        var rows = await repo.GetEntriesForPersonsAsync(
            [p2, p1],
            from: new DateOnly(2026, 02, 02),
            toExclusive: new DateOnly(2026, 02, 06));

        // Assert
        // expected overlaps:
        // p1: [02-01..02-05) overlaps, [02-05..null) overlaps
        // p2: [02-02..02-06) overlaps
        Assert.Equal(3, rows.Count);

        Assert.Equal(p1, rows[0].PersonId);
        Assert.Equal(new DateOnly(2026, 02, 01), rows[0].From);

        Assert.Equal(p1, rows[1].PersonId);
        Assert.Equal(new DateOnly(2026, 02, 05), rows[1].From);

        Assert.Equal(p2, rows[2].PersonId);
        Assert.Equal(new DateOnly(2026, 02, 02), rows[2].From);

        Assert.DoesNotContain(rows, x => x.IsDeleted);
    }

    //======================================================================
    // GetEntriesForPersonAsync
    //======================================================================

    /// <summary>
    /// Валідує аргументи для GetEntriesForPersonAsync.
    /// </summary>
    [Fact]
    public async Task GetEntriesForPersonAsync_ValidatesArguments()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryQueryRepository(testDb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetEntriesForPersonAsync(Guid.Empty, new DateOnly(2026, 02, 01), new DateOnly(2026, 02, 02)));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetEntriesForPersonAsync(Guid.NewGuid(), default, new DateOnly(2026, 02, 02)));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetEntriesForPersonAsync(Guid.NewGuid(), new DateOnly(2026, 02, 01), default));

        var empty = await repo.GetEntriesForPersonAsync(Guid.NewGuid(), new DateOnly(2026, 02, 02), new DateOnly(2026, 02, 02));
        Assert.Empty(empty);
    }

    /// <summary>
    /// Повертає записи для 1 особи, що перетинають діапазон, та ігнорує soft-deleted.
    /// </summary>
    [Fact]
    public async Task GetEntriesForPersonAsync_ReturnsOverlaps_Ordered_AndIgnoresDeleted()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryQueryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var codeId = await SeedCodeAsync(testDb, "T", now);

        var personId = Guid.NewGuid();
        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeId,
            From = new DateOnly(2026, 02, 01),
            To = new DateOnly(2026, 02, 03),
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false
        });

        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeId,
            From = new DateOnly(2026, 02, 03),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false
        });

        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeId,
            From = new DateOnly(2026, 02, 02),
            To = new DateOnly(2026, 02, 04),
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = true,
            DeletedBy = "seed",
            DeletedAtUtc = now,
            DeleteReason = "test"
        });

        // Act: [02-02..02-04)
        var rows = await repo.GetEntriesForPersonAsync(personId, new DateOnly(2026, 02, 02), new DateOnly(2026, 02, 04));

        // Assert: 2 non-deleted entries overlap
        Assert.Equal(2, rows.Count);
        Assert.Equal(new DateOnly(2026, 02, 01), rows[0].From);
        Assert.Equal(new DateOnly(2026, 02, 03), rows[1].From);
        Assert.DoesNotContain(rows, x => x.IsDeleted);
    }

    //======================================================================
    // GetByIdAsync
    //======================================================================

    /// <summary>
    /// GetByIdAsync повертає null для soft-deleted.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenDeleted()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryQueryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var codeId = await SeedCodeAsync(testDb, "T", now);

        var personId = Guid.NewGuid();
        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        var entryId = Guid.NewGuid();
        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = entryId,
            TimesheetId = tsId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeId,
            From = new DateOnly(2026, 02, 01),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = true,
            DeletedBy = "seed",
            DeletedAtUtc = now,
            DeleteReason = "test"
        });

        var found = await repo.GetByIdAsync(entryId);
        Assert.Null(found);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetByIdAsync(Guid.Empty));
    }

    //======================================================================
    // GetActiveEntryOnDateAsync
    //======================================================================

    /// <summary>
    /// Якщо епізоду на дату немає — повертає null.
    /// </summary>
    [Fact]
    public async Task GetActiveEntryOnDateAsync_ReturnsNull_WhenNoEpisodeCoveringDate()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryQueryRepository(testDb.Factory);

        var personId = Guid.NewGuid();
        var entry = await repo.GetActiveEntryOnDateAsync(personId, new DateOnly(2026, 02, 10));
        Assert.Null(entry);
    }

    /// <summary>
    /// Повертає активний запис на дату з урахуванням half-open семантики:
    /// <c>From &lt;= date</c> і <c>date &lt; To</c>.
    /// Завантажує навігацію <see cref="TimesheetCodeDefinition"/>.
    /// </summary>
    [Fact]
    public async Task GetActiveEntryOnDateAsync_ReturnsCorrectEntry_AndIncludesCodeDefinition()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryQueryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var codeT = await SeedCodeAsync(testDb, "T", now);
        var code30 = await SeedCodeAsync(testDb, "30", now);

        var personId = Guid.NewGuid();
        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), new DateOnly(2026, 02, 20), "seed", now);

        // [02-01..02-10)
        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeT,
            From = new DateOnly(2026, 02, 01),
            To = new DateOnly(2026, 02, 10),
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false,
            Reference = "DocA"
        });

        // [02-10..null)
        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsId,
            PersonId = personId,
            TimesheetCodeDefinitionId = code30,
            From = new DateOnly(2026, 02, 10),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false,
            Reference = "DocB"
        });

        // date inside first interval
        var onFeb09 = await repo.GetActiveEntryOnDateAsync(personId, new DateOnly(2026, 02, 09));
        Assert.NotNull(onFeb09);
        Assert.Equal(codeT, onFeb09!.TimesheetCodeDefinitionId);
        Assert.NotNull(onFeb09.TimesheetCodeDefinition);
        Assert.Equal("T", onFeb09.TimesheetCodeDefinition!.Code);

        // boundary: 02-10 belongs to second interval (To is exclusive)
        var onFeb10 = await repo.GetActiveEntryOnDateAsync(personId, new DateOnly(2026, 02, 10));
        Assert.NotNull(onFeb10);
        Assert.Equal(code30, onFeb10!.TimesheetCodeDefinitionId);
        Assert.NotNull(onFeb10.TimesheetCodeDefinition);
        Assert.Equal("30", onFeb10.TimesheetCodeDefinition!.Code);
    }

    /// <summary>
    /// Ігнорує soft-deleted записи.
    /// </summary>
    [Fact]
    public async Task GetActiveEntryOnDateAsync_IgnoresDeletedEntries()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryQueryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var codeT = await SeedCodeAsync(testDb, "T", now);

        var personId = Guid.NewGuid();
        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeT,
            From = new DateOnly(2026, 02, 01),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = true,
            DeletedBy = "seed",
            DeletedAtUtc = now,
            DeleteReason = "test"
        });

        var active = await repo.GetActiveEntryOnDateAsync(personId, new DateOnly(2026, 02, 10));
        Assert.Null(active);
    }

    /// <summary>
    /// Якщо епізоди накладаються на дату (пошкоджені дані) — кидає (SingleOrDefault).
    /// </summary>
    [Fact]
    public async Task GetActiveEntryOnDateAsync_Throws_WhenEpisodesOverlapOnDate()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryQueryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var codeT = await SeedCodeAsync(testDb, "T", now);

        var personId = Guid.NewGuid();

        // Two closed episodes overlap 2026-02-10 (allowed by DB, but is corrupted state for reads)
        var ts1 = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), new DateOnly(2026, 02, 15), "seed", now);
        var ts2 = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 10), new DateOnly(2026, 02, 20), "seed", now);

        // Seed at least one entry (not required for throw; the throw comes from resolving episode)
        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = ts1,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeT,
            From = new DateOnly(2026, 02, 01),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.GetActiveEntryOnDateAsync(personId, new DateOnly(2026, 02, 10)));

        _ = ts2; // explicit: ts2 exists to create overlap
    }

    //======================================================================
    // GetNextEntryAfterDateAsync
    //======================================================================

    /// <summary>
    /// Повертає наступний запис (From &gt; date) в межах епізоду, що покриває дату.
    /// </summary>
    [Fact]
    public async Task GetNextEntryAfterDateAsync_ReturnsNextEntryWithinEpisode()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryQueryRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var codeT = await SeedCodeAsync(testDb, "T", now);
        var code30 = await SeedCodeAsync(testDb, "30", now);

        var personId = Guid.NewGuid();
        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeT,
            From = new DateOnly(2026, 02, 01),
            To = new DateOnly(2026, 02, 10),
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false
        });

        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsId,
            PersonId = personId,
            TimesheetCodeDefinitionId = code30,
            From = new DateOnly(2026, 02, 10),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false
        });

        var next = await repo.GetNextEntryAfterDateAsync(personId, new DateOnly(2026, 02, 09));
        Assert.NotNull(next);
        Assert.Equal(new DateOnly(2026, 02, 10), next!.From);

        var none = await repo.GetNextEntryAfterDateAsync(personId, new DateOnly(2026, 02, 10));
        Assert.Null(none);
    }

    /// <summary>
    /// Якщо епізоду на дату немає — повертає null.
    /// </summary>
    [Fact]
    public async Task GetNextEntryAfterDateAsync_ReturnsNull_WhenNoEpisodeCoveringDate()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryQueryRepository(testDb.Factory);

        var personId = Guid.NewGuid();
        var next = await repo.GetNextEntryAfterDateAsync(personId, new DateOnly(2026, 02, 10));
        Assert.Null(next);
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static DateTime Utc(int y, int m, int d, int hh, int mm)
        => new(y, m, d, hh, mm, 0, DateTimeKind.Utc);

    /// <summary>
    /// Сідить TimesheetCodeDefinition (IsActive=true).
    /// </summary>
    private static async Task<Guid> SeedCodeAsync(SqliteTestDb testDb, string code, DateTime nowUtc)
    {
        await using var db = await testDb.Factory.CreateDbContextAsync();

        var existing = await db.TimesheetCodes.SingleOrDefaultAsync(x => x.Code == code);
        if (existing is not null)
            return existing.Id;

        var e = new TimesheetCodeDefinition
        {
            Id = Guid.NewGuid(),
            Code = code,
            Title = code.Trim(),
            Description = null,
            SortOrder = 0,
            Priority = 0,
            IsTerminal = false,
            IsActive = true,
            CreatedBy = "seed",
            CreatedAtUtc = nowUtc
        };

        db.TimesheetCodes.Add(e);
        await db.SaveChangesAsync();

        return e.Id;
    }

    /// <summary>
    /// Сідить епізод табеля.
    /// </summary>
    private static async Task<Guid> SeedEpisodeAsync(
        SqliteTestDb testDb,
        Guid personId,
        DateOnly openedAt,
        DateOnly? closedAt,
        string createdBy,
        DateTime nowUtc)
    {
        var ep = new TimeSheetAggregate
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            OpenedAt = openedAt,
            ClosedAt = closedAt,
            CreatedBy = createdBy,
            CreatedAtUtc = nowUtc
        };

        await using var db = await testDb.Factory.CreateDbContextAsync();
        db.TimeSheets.Add(ep);
        await db.SaveChangesAsync();

        return ep.Id;
    }

    /// <summary>
    /// Додає entry напряму в БД.
    /// </summary>
    private static async Task SeedEntryAsync(SqliteTestDb testDb, TimesheetEntry entry)
    {
        await using var db = await testDb.Factory.CreateDbContextAsync();
        db.TimesheetEntries.Add(entry);
        await db.SaveChangesAsync();
    }
}
