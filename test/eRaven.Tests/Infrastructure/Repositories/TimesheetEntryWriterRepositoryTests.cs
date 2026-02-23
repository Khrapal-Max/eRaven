//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntryWriterRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

/// <summary>
/// Тести для <see cref="TimesheetEntryWriterRepository"/>.
///
/// <para>
/// Фіксуємо контракт writer-операцій над подіями епізоду (<see cref="TimeSheetAggregate"/>):
/// <list type="bullet">
/// <item><description>операції виконуються <b>тільки</b> через методи агрегату (жодних ручних правок інтервалів);</description></item>
/// <item><description>семантика дат — <c>[From..To)</c>, <c>To</c> — <b>exclusive</b>;</description></item>
/// <item><description>owner-check: корекція/видалення можливі лише для entry поточної особи;</description></item>
/// <item><description>TransitionAsync: якщо anchor вже існує — робить replace-in-place; інакше додає новий change-point;</description></item>
/// <item><description>ApplyChangePointsAsync нормалізує вхід: сортує, відкидає невалідні, бере останню точку на дату;</description></item>
/// <item><description>SoftDeleteAsync встановлює audit-поля та нормалізує шкалу ігноруючи deleted записи.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class TimesheetEntryWriterRepositoryTests
{
    //======================================================================
    // AddEntryAsync
    //======================================================================

    /// <summary>
    /// AddEntryAsync валідуює author/personId/codeId/effectiveAt.
    /// </summary>
    [Fact]
    public async Task AddEntryAsync_ValidatesArguments()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryWriterRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var codeId = await SeedCodeAsync(testDb, "T", now);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.AddEntryAsync(Guid.NewGuid(), new DateOnly(2026, 02, 10), codeId, null, " ", now));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.AddEntryAsync(Guid.Empty, new DateOnly(2026, 02, 10), codeId, null, "admin", now));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.AddEntryAsync(Guid.NewGuid(), new DateOnly(2026, 02, 10), Guid.Empty, null, "admin", now));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.AddEntryAsync(Guid.NewGuid(), default, codeId, null, "admin", now));
    }

    /// <summary>
    /// Якщо епізоду на дату немає — кидає.
    /// </summary>
    [Fact]
    public async Task AddEntryAsync_Throws_WhenEpisodeNotFoundOnDate()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryWriterRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var codeId = await SeedCodeAsync(testDb, "T", now);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.AddEntryAsync(Guid.NewGuid(), new DateOnly(2026, 02, 10), codeId, null, "admin", now));
    }

    /// <summary>
    /// Додає подію у епізод і повертає Id створеного entry.
    /// </summary>
    [Fact]
    public async Task AddEntryAsync_CreatesEntry_AndReturnsId()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryWriterRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var codeId = await SeedCodeAsync(testDb, "T", now);

        var personId = Guid.NewGuid();
        _ = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        var id = await repo.AddEntryAsync(
            personId,
            effectiveAt: new DateOnly(2026, 02, 10),
            codeId: codeId,
            reference: "Doc#1",
            author: " admin ",
            nowUtc: now);

        Assert.NotEqual(Guid.Empty, id);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var e = await db.TimesheetEntries.SingleAsync(x => x.Id == id);

        Assert.Equal(personId, e.PersonId);
        Assert.Equal(codeId, e.TimesheetCodeDefinitionId);
        Assert.Equal(new DateOnly(2026, 02, 10), e.From);
        Assert.Equal("Doc#1", e.Reference);
        Assert.Equal("admin", e.CreatedBy);
        Assert.Equal(now, e.CreatedAtUtc);
    }

    //======================================================================
    // CorrectEntryAsync / RemoveEntryAsync
    //======================================================================

    /// <summary>
    /// CorrectEntryAsync перевіряє owner: не можна коригувати entry іншої особи.
    /// </summary>
    [Fact]
    public async Task CorrectEntryAsync_Throws_WhenEntryBelongsToAnotherPerson()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryWriterRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var codeT = await SeedCodeAsync(testDb, "T", now);
        var code30 = await SeedCodeAsync(testDb, "30", now);

        var p1 = Guid.NewGuid();
        _ = await SeedEpisodeAsync(testDb, p1, new DateOnly(2026, 02, 01), null, "seed", now);

        var entryId = await repo.AddEntryAsync(p1, new DateOnly(2026, 02, 10), codeT, null, "seed", now);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CorrectEntryAsync(
                personId: Guid.NewGuid(),
                entryId: entryId,
                nextCodeId: code30,
                nextEffectiveAt: new DateOnly(2026, 02, 11),
                reference: "X",
                author: "admin",
                nowUtc: now));
    }

    /// <summary>
    /// CorrectEntryAsync оновлює код/дату/референс та audit-поля через агрегат.
    /// </summary>
    [Fact]
    public async Task CorrectEntryAsync_UpdatesEntry_AndSetsAudit()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryWriterRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var later = Utc(2026, 02, 17, 12, 00);
        var codeT = await SeedCodeAsync(testDb, "T", now);
        var code30 = await SeedCodeAsync(testDb, "30", now);

        var personId = Guid.NewGuid();
        _ = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        var entryId = await repo.AddEntryAsync(personId, new DateOnly(2026, 02, 10), codeT, null, "seed", now);

        await repo.CorrectEntryAsync(
            personId,
            entryId,
            nextCodeId: code30,
            nextEffectiveAt: new DateOnly(2026, 02, 12),
            reference: "Doc#2",
            author: " admin ",
            nowUtc: later);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var e = await db.TimesheetEntries.SingleAsync(x => x.Id == entryId);

        Assert.Equal(code30, e.TimesheetCodeDefinitionId);
        Assert.Equal(new DateOnly(2026, 02, 12), e.From);
        Assert.Equal("Doc#2", e.Reference);
        Assert.Equal("admin", e.UpdatedBy);
        Assert.Equal(later, e.UpdatedAtUtc);
    }

    /// <summary>
    /// RemoveEntryAsync фізично видаляє entry з епізоду.
    /// </summary>
    [Fact]
    public async Task RemoveEntryAsync_RemovesEntry()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryWriterRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var codeT = await SeedCodeAsync(testDb, "T", now);

        var personId = Guid.NewGuid();
        _ = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        var entryId = await repo.AddEntryAsync(personId, new DateOnly(2026, 02, 10), codeT, null, "seed", now);

        await repo.RemoveEntryAsync(personId, entryId, "admin", now);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var exists = await db.TimesheetEntries.AnyAsync(x => x.Id == entryId);
        Assert.False(exists);
    }

    //======================================================================
    // TransitionAsync
    //======================================================================

    /// <summary>
    /// Якщо anchor-event уже існує на effectiveAt — робить replace-in-place (Correction) і не створює новий entry.
    /// </summary>
    [Fact]
    public async Task TransitionAsync_WhenAnchorExistsOnDate_ReplacesInPlace()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryWriterRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var later = Utc(2026, 02, 17, 11, 00);
        var codeT = await SeedCodeAsync(testDb, "T", now);
        var code30 = await SeedCodeAsync(testDb, "30", now);

        var personId = Guid.NewGuid();
        _ = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        var entryId = await repo.AddEntryAsync(personId, new DateOnly(2026, 02, 10), codeT, "A", "seed", now);

        await repo.TransitionAsync(personId, new DateOnly(2026, 02, 10), code30, "B", "admin", later);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var entries = await db.TimesheetEntries.Where(x => x.PersonId == personId).ToListAsync();
        Assert.Single(entries);

        var e = entries[0];
        Assert.Equal(entryId, e.Id);
        Assert.Equal(code30, e.TimesheetCodeDefinitionId);
        Assert.Equal(new DateOnly(2026, 02, 10), e.From);
        Assert.Equal("B", e.Reference);
        Assert.Equal("admin", e.UpdatedBy);
        Assert.Equal(later, e.UpdatedAtUtc);
    }

    /// <summary>
    /// Якщо anchor-event уже існує і значення не змінилось — TransitionAsync є no-op (UpdatedBy/UpdatedAtUtc не чіпає).
    /// </summary>
    [Fact]
    public async Task TransitionAsync_WhenAnchorExistsOnDate_AndNoChanges_DoesNothing()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryWriterRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var later = Utc(2026, 02, 17, 11, 00);
        var codeT = await SeedCodeAsync(testDb, "T", now);

        var personId = Guid.NewGuid();
        _ = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        var entryId = await repo.AddEntryAsync(personId, new DateOnly(2026, 02, 10), codeT, "A", "seed", now);

        await repo.TransitionAsync(personId, new DateOnly(2026, 02, 10), codeT, "A", "admin", later);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var e = await db.TimesheetEntries.SingleAsync(x => x.Id == entryId);

        Assert.Null(e.UpdatedBy);
        Assert.Null(e.UpdatedAtUtc);
    }

    /// <summary>
    /// Якщо на дату немає anchor-event — додає новий change-point і clamp’ить попередній інтервал (To = effectiveAt).
    /// </summary>
    [Fact]
    public async Task TransitionAsync_WhenNoAnchorOnDate_AddsEntry_AndClampsPrevious()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryWriterRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var codeT = await SeedCodeAsync(testDb, "T", now);
        var code30 = await SeedCodeAsync(testDb, "30", now);

        var personId = Guid.NewGuid();
        _ = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        var firstId = await repo.AddEntryAsync(personId, new DateOnly(2026, 02, 01), codeT, null, "seed", now);

        await repo.TransitionAsync(personId, new DateOnly(2026, 02, 10), code30, null, "admin", now);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var rows = await db.TimesheetEntries
            .Where(x => x.PersonId == personId)
            .OrderBy(x => x.From)
            .ToListAsync();

        Assert.Equal(2, rows.Count);
        Assert.Equal(firstId, rows[0].Id);
        Assert.Equal(new DateOnly(2026, 02, 01), rows[0].From);
        Assert.Equal(new DateOnly(2026, 02, 10), rows[0].To); // exclusive

        Assert.Equal(new DateOnly(2026, 02, 10), rows[1].From);
        Assert.Null(rows[1].To);
        Assert.Equal(code30, rows[1].TimesheetCodeDefinitionId);
    }

    //======================================================================
    // ApplyChangePointsAsync
    //======================================================================

    /// <summary>
    /// Якщо points порожній або весь невалідний — метод завершується без доступу до БД.
    /// </summary>
    [Fact]
    public async Task ApplyChangePointsAsync_ReturnsEarly_WhenPointsEmptyOrInvalid()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryWriterRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();

        // empty
        await repo.ApplyChangePointsAsync(personId, [], "admin", now);

        // invalid-only (default date / empty guid)
        await repo.ApplyChangePointsAsync(
            personId,
            [
                (default, Guid.NewGuid(), null),
                (new DateOnly(2026, 02, 10), Guid.Empty, null)
            ],
            "admin",
            now);
    }

    /// <summary>
    /// Нормалізує вхідні точки: сортує, бере останню точку на дату, додає/коригує без no-op.
    /// </summary>
    [Fact]
    public async Task ApplyChangePointsAsync_AddsAndCorrects_AndKeepsLastPerDate()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryWriterRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var codeT = await SeedCodeAsync(testDb, "T", now);
        var code30 = await SeedCodeAsync(testDb, "30", now);
        var code40 = await SeedCodeAsync(testDb, "40", now);

        var personId = Guid.NewGuid();
        _ = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        // existing anchor
        _ = await repo.AddEntryAsync(personId, new DateOnly(2026, 02, 01), codeT, string.Empty, "seed", now);

        var points = new List<(DateOnly EffectiveAt, Guid CodeId, string? Reference)>
        {
            (new DateOnly(2026, 02, 10), code30, null),
            (default, code30, "invalid"),
            (new DateOnly(2026, 02, 05), code30, "X"),
            (new DateOnly(2026, 02, 05), code40, "Y"), // last wins for 02-05
            (new DateOnly(2026, 02, 01), codeT, "") // no-op
        };

        await repo.ApplyChangePointsAsync(personId, points, "admin", now);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var rows = await db.TimesheetEntries
            .Where(x => x.PersonId == personId && !x.IsDeleted)
            .OrderBy(x => x.From)
            .ToListAsync();

        Assert.Equal(3, rows.Count);

        Assert.Equal(new DateOnly(2026, 02, 01), rows[0].From);
        Assert.Equal(new DateOnly(2026, 02, 05), rows[0].To);

        Assert.Equal(new DateOnly(2026, 02, 05), rows[1].From);
        Assert.Equal(code40, rows[1].TimesheetCodeDefinitionId);
        Assert.Equal("Y", rows[1].Reference);
        Assert.Equal(new DateOnly(2026, 02, 10), rows[1].To);

        Assert.Equal(new DateOnly(2026, 02, 10), rows[2].From);
        Assert.Equal(code30, rows[2].TimesheetCodeDefinitionId);
        Assert.Equal(string.Empty, rows[2].Reference);
        Assert.Null(rows[2].To);
    }

    //======================================================================
    // SoftDeleteAsync
    //======================================================================

    /// <summary>
    /// SoftDeleteAsync виставляє audit для entry та нормалізує шкалу ігноруючи deleted записи.
    /// </summary>
    [Fact]
    public async Task SoftDeleteAsync_SoftDeletesEntry_AndNormalizesTimeline()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryWriterRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var codeT = await SeedCodeAsync(testDb, "T", now);
        var code30 = await SeedCodeAsync(testDb, "30", now);

        var personId = Guid.NewGuid();
        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        var e1 = await repo.AddEntryAsync(personId, new DateOnly(2026, 02, 01), codeT, null, "seed", now);
        var e2 = await repo.AddEntryAsync(personId, new DateOnly(2026, 02, 05), code30, null, "seed", now);

        await repo.SoftDeleteAsync(e2, reason: " excluded ", author: " admin ", nowUtc: now);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var ep = await db.TimeSheets.Include(x => x.Entries).SingleAsync(x => x.Id == tsId);

        var first = ep.Entries.Single(x => x.Id == e1);
        var deleted = ep.Entries.Single(x => x.Id == e2);

        Assert.True(deleted.IsDeleted);
        Assert.Equal("admin", deleted.DeletedBy);
        Assert.Equal(now, deleted.DeletedAtUtc);
        Assert.Equal("excluded", deleted.DeleteReason);

        // deleted entry ignored by NormalizeEntries => first becomes last
        Assert.Null(first.To);
    }

    //======================================================================
    // UpdateAsync / SaveTransitionAsync
    //======================================================================

    /// <summary>
    /// UpdateAsync очікує UpdatedBy/UpdatedAtUtc на переданій сутності та коригує через агрегат.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ValidatesAudit_AndUpdatesThroughAggregate()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryWriterRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var later = Utc(2026, 02, 17, 12, 00);
        var codeT = await SeedCodeAsync(testDb, "T", now);
        var code30 = await SeedCodeAsync(testDb, "30", now);

        var personId = Guid.NewGuid();
        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);
        var entryId = await repo.AddEntryAsync(personId, new DateOnly(2026, 02, 10), codeT, "A", "seed", now);

        // invalid: missing audit
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.UpdateAsync(new TimesheetEntry
            {
                Id = entryId,
                TimesheetId = tsId,
                PersonId = personId,
                TimesheetCodeDefinitionId = code30,
                From = new DateOnly(2026, 02, 10),
                Reference = "B"
            }));

        // valid
        await repo.UpdateAsync(new TimesheetEntry
        {
            Id = entryId,
            TimesheetId = tsId,
            PersonId = personId,
            TimesheetCodeDefinitionId = code30,
            From = new DateOnly(2026, 02, 11),
            Reference = "B",
            UpdatedBy = " admin ",
            UpdatedAtUtc = later
        });

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var e = await db.TimesheetEntries.SingleAsync(x => x.Id == entryId);
        Assert.Equal(code30, e.TimesheetCodeDefinitionId);
        Assert.Equal(new DateOnly(2026, 02, 11), e.From);
        Assert.Equal("B", e.Reference);
        Assert.Equal("admin", e.UpdatedBy);
        Assert.Equal(later, e.UpdatedAtUtc);
    }

    /// <summary>
    /// SaveTransitionAsync торкає audit prevUpdated та додає nextAdded з фіксованим Id.
    /// </summary>
    [Fact]
    public async Task SaveTransitionAsync_TouchesPrevAndAddsNextWithProvidedId()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEntryWriterRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var later = Utc(2026, 02, 17, 11, 00);
        var codeT = await SeedCodeAsync(testDb, "T", now);
        var code30 = await SeedCodeAsync(testDb, "30", now);

        var personId = Guid.NewGuid();
        var tsId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        var prevId = await repo.AddEntryAsync(personId, new DateOnly(2026, 02, 01), codeT, null, "seed", now);

        var nextId = Guid.NewGuid();

        await repo.SaveTransitionAsync(
            prevUpdated: new TimesheetEntry
            {
                Id = prevId,
                TimesheetId = tsId,
                UpdatedBy = " admin ",
                UpdatedAtUtc = later
            },
            nextAdded: new TimesheetEntry
            {
                Id = nextId,
                TimesheetId = tsId,
                PersonId = personId,
                TimesheetCodeDefinitionId = code30,
                From = new DateOnly(2026, 02, 10),
                Reference = "Doc#1",
                Note = "note",
                CreatedBy = " admin ",
                CreatedAtUtc = later
            });

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var rows = await db.TimesheetEntries
            .Where(x => x.PersonId == personId)
            .OrderBy(x => x.From)
            .ToListAsync();

        Assert.Equal(2, rows.Count);

        var prev = rows[0];
        var next = rows[1];

        Assert.Equal(prevId, prev.Id);
        Assert.Equal("admin", prev.UpdatedBy);
        Assert.Equal(later, prev.UpdatedAtUtc);
        Assert.Equal(new DateOnly(2026, 02, 10), prev.To); // exclusive, clamped by NormalizeEntries

        Assert.Equal(nextId, next.Id);
        Assert.Equal(code30, next.TimesheetCodeDefinitionId);
        Assert.Equal(new DateOnly(2026, 02, 10), next.From);
        Assert.Equal("Doc#1", next.Reference);
        Assert.Equal("note", next.Note);
        Assert.Equal("admin", next.CreatedBy);
        Assert.Equal(later, next.CreatedAtUtc);
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
}
