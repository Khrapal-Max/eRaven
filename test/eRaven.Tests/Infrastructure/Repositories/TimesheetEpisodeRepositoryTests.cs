//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEpisodeRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

/// <summary>
/// Тести для <see cref="TimesheetEpisodeRepository"/>.
///
/// <para>
/// Фіксуємо контракт життєвого циклу епізоду табеля:
/// <list type="bullet">
/// <item><description>пошук епізоду на дату / активного епізоду;</description></item>
/// <item><description>tracked-load на дату включає TaskSpans;</description></item>
/// <item><description>OpenOnEnrollAsync створює новий епізод + дефолтний entry ("Т") і є ідемпотентним;</description></item>
/// <item><description>ValidateCanCloseOnExcludeAsync контролює дозволений стан на closeTo ("Т" / "РОЗПОР");</description></item>
/// <item><description>CloseOnExcludeAsync закриває епізод, clamp’ить entries, soft-delete’ить future entries.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class TimesheetEpisodeRepositoryTests
{
    //======================================================================
    // Reads
    //======================================================================

    /// <summary>
    /// GetEpisodeOnDateAsync повертає епізод, що покриває дату.
    /// Якщо епізодів декілька, бере найсвіжіший за OpenedAt/Id.
    /// </summary>
    [Fact]
    public async Task GetEpisodeOnDateAsync_ReturnsEpisodeCoveringDate()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();

        // episode 1: [2026-01-01 .. 2026-01-31]
        var ep1 = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 01, 01), new DateOnly(2026, 01, 31), "seed", now);

        // episode 2: [2026-02-01 .. null] (active)
        var ep2 = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        var onJan = await repo.GetEpisodeOnDateAsync(personId, new DateOnly(2026, 01, 15));
        Assert.NotNull(onJan);
        Assert.Equal(ep1, onJan!.Id);

        var onFeb = await repo.GetEpisodeOnDateAsync(personId, new DateOnly(2026, 02, 10));
        Assert.NotNull(onFeb);
        Assert.Equal(ep2, onFeb!.Id);

        var none = await repo.GetEpisodeOnDateAsync(personId, new DateOnly(2025, 12, 31));
        Assert.Null(none);
    }

    /// <summary>
    /// GetActiveEpisodeAsync повертає активний (ClosedAt == null) епізод.
    /// </summary>
    [Fact]
    public async Task GetActiveEpisodeAsync_ReturnsActiveEpisode()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();

        await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 01, 01), new DateOnly(2026, 01, 31), "seed", now);
        var activeId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        var active = await repo.GetActiveEpisodeAsync(personId);
        Assert.NotNull(active);
        Assert.Equal(activeId, active!.Id);
        Assert.Null(active.ClosedAt);
    }

    /// <summary>
    /// LoadEpisodeOnDateForUpdateAsync повертає tracked епізод із підвантаженими TaskSpans (Include).
    /// </summary>
    [Fact]
    public async Task LoadEpisodeOnDateForUpdateAsync_IncludesTaskSpans()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();

        // Seed episode with one span via aggregate method (to keep invariants)
        await SeedEpisodeWithSpanAsync(
            testDb,
            personId,
            openedAt: new DateOnly(2026, 02, 01),
            spanFrom: new DateOnly(2026, 02, 10),
            nowUtc: now);

        var loaded = await repo.LoadEpisodeOnDateForUpdateAsync(personId, new DateOnly(2026, 02, 10));

        Assert.NotNull(loaded);
        Assert.NotNull(loaded!.TaskSpans);
        Assert.Single(loaded.TaskSpans);
    }

    //======================================================================
    // OpenOnEnrollAsync
    //======================================================================

    /// <summary>
    /// OpenOnEnrollAsync створює новий епізод, якщо активного немає,
    /// та додає дефолтний entry з кодом "Т" на дату зарахування (ідемпотентно).
    /// </summary>
    [Fact]
    public async Task OpenOnEnrollAsync_CreatesEpisode_AndDefaultEntry()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 02, 10);

        // Default enroll code must exist and be active
        var codeT = await SeedCodeAsync(testDb, TimesheetSystemCodes.BaseState);

        await repo.OpenOnEnrollAsync(personId, enrollDate, "  duty  ", now);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        var ep = await db.TimeSheets.SingleAsync(x => x.PersonId == personId);
        Assert.Equal(enrollDate, ep.OpenedAt);
        Assert.Null(ep.ClosedAt);
        Assert.Equal("duty", ep.CreatedBy);
        Assert.Equal(now, ep.CreatedAtUtc);

        var entry = await db.TimesheetEntries
            .Include(x => x.TimesheetCodeDefinition)
            .SingleAsync(x => x.TimesheetId == ep.Id && !x.IsDeleted);

        Assert.Equal(personId, entry.PersonId);
        Assert.Equal(codeT, entry.TimesheetCodeDefinitionId);
        Assert.Equal(enrollDate, entry.From);
        Assert.Null(entry.To);
        Assert.Equal("Auto: enroll", entry.Reference);
        Assert.Equal("duty", entry.CreatedBy);
        Assert.Equal(now, entry.CreatedAtUtc);

        Assert.NotNull(entry.TimesheetCodeDefinition);
        Assert.Equal(TimesheetSystemCodes.BaseState, entry.TimesheetCodeDefinition!.Code);
    }

    /// <summary>
    /// OpenOnEnrollAsync ідемпотентний:
    /// якщо активний епізод уже існує і entry на enrollDate покриває дату — нічого не додає.
    /// </summary>
    [Fact]
    public async Task OpenOnEnrollAsync_IsIdempotent_WhenAlreadyOpenedAndHasEntry()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 02, 10);

        await SeedCodeAsync(testDb, TimesheetSystemCodes.BaseState);

        await repo.OpenOnEnrollAsync(personId, enrollDate, "duty", now);
        await repo.OpenOnEnrollAsync(personId, enrollDate, "duty", now.AddMinutes(1)); // second call

        await using var db = await testDb.Factory.CreateDbContextAsync();

        Assert.Equal(1, await db.TimeSheets.CountAsync(x => x.PersonId == personId));
        Assert.Equal(1, await db.TimesheetEntries.CountAsync(x => x.PersonId == personId && !x.IsDeleted));
    }

    /// <summary>
    /// OpenOnEnrollAsync кидає, якщо активний епізод відкритий пізніше, ніж enrollDate
    /// (не можна "перевідкривати" в минулому).
    /// </summary>
    [Fact]
    public async Task OpenOnEnrollAsync_Throws_WhenActiveEpisodeOpenedLaterThanEnrollDate()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();

        await SeedCodeAsync(testDb, TimesheetSystemCodes.BaseState);

        // Active episode opened at 2026-02-20
        await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 20), null, "seed", now);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.OpenOnEnrollAsync(personId, new DateOnly(2026, 02, 10), "duty", now));

        Assert.Contains("активний епізод відкритий пізніше", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// OpenOnEnrollAsync кидає, якщо немає активного епізоду, але enrollDate <= lastClosed
    /// (не можна відкривати новий епізод "заднім числом" поверх закритих).
    /// </summary>
    [Fact]
    public async Task OpenOnEnrollAsync_Throws_WhenEnrollDateOverlapsLastClosed()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();

        await SeedCodeAsync(testDb, TimesheetSystemCodes.BaseState);

        // Closed episode ends at 2026-02-10
        await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), new DateOnly(2026, 02, 10), "seed", now);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.OpenOnEnrollAsync(personId, new DateOnly(2026, 02, 10), "duty", now));

        Assert.Contains("не можна накладати", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    //======================================================================
    // ValidateCanCloseOnExcludeAsync
    //======================================================================

    /// <summary>
    /// ValidateCanCloseOnExcludeAsync кидає, якщо активного епізоду немає.
    /// </summary>
    [Fact]
    public async Task ValidateCanCloseOnExcludeAsync_Throws_WhenNoActiveEpisode()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(testDb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.ValidateCanCloseOnExcludeAsync(Guid.NewGuid(), new DateOnly(2026, 02, 10)));

        Assert.Contains("немає активного епізоду", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// ValidateCanCloseOnExcludeAsync кидає, якщо на closeTo немає активного entry (дані пошкоджені).
    /// </summary>
    [Fact]
    public async Task ValidateCanCloseOnExcludeAsync_Throws_WhenNoActiveEntryOnCloseDate()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();

        await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.ValidateCanCloseOnExcludeAsync(personId, new DateOnly(2026, 02, 10)));

        Assert.Contains("немає активного запису", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// ValidateCanCloseOnExcludeAsync кидає, якщо код на closeTo не дозволений (не "Т" і не "РОЗПОР та не 30").
    /// </summary>
    [Fact]
    public async Task ValidateCanCloseOnExcludeAsync_Throws_WhenCodeNotAllowed()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();
        var closeTo = new DateOnly(2026, 02, 10);

        var code30 = await SeedCodeAsync(testDb, TimesheetSystemCodes.LeaveWound);
        var epId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = epId,
            PersonId = personId,
            TimesheetCodeDefinitionId = code30,
            From = new DateOnly(2026, 02, 01),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.ValidateCanCloseOnExcludeAsync(personId, closeTo));

        Assert.Contains("Дозволено тільки", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// ValidateCanCloseOnExcludeAsync дозволяє закриття, якщо код на closeTo == "Т" або "РОЗПОР".
    /// Додатково фіксуємо, що порівняння робиться з Trim (Code може мати пробіли).
    /// </summary>
    [Fact]
    public async Task ValidateCanCloseOnExcludeAsync_Allows_WhenAllowedCodeTrimmed()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();
        var closeTo = new DateOnly(2026, 02, 10);

        // Seed a "trimmed" code: "  Т  "
        var codeId = await SeedCodeAsync(testDb, "  " + TimesheetSystemCodes.BaseState + "  ");

        var epId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = epId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeId,
            From = new DateOnly(2026, 02, 01),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false
        });

        // should not throw
        await repo.ValidateCanCloseOnExcludeAsync(personId, closeTo);
    }

    //======================================================================
    // CloseOnExcludeAsync
    //======================================================================

    /// <summary>
    /// CloseOnExcludeAsync:
    /// <list type="bullet">
    /// <item><description>закриває активний епізод (ClosedAt, ClosedBy, ClosedAtUtc);</description></item>
    /// <item><description>clamp’ить entries які виходять за closeTo (або open-ended) до To=closeTo;</description></item>
    /// <item><description>soft-delete’ить future entries (From > closeTo) з причиною;</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task CloseOnExcludeAsync_ClosesEpisode_ClampsEntries_AndSoftDeletesFuture()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();
        var closeTo = new DateOnly(2026, 02, 10);

        var codeT = await SeedCodeAsync(testDb, TimesheetSystemCodes.BaseState);

        var epId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", now);

        // entry1: [02-01..null] -> must clamp to 02-10
        var e1 = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = epId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeT,
            From = new DateOnly(2026, 02, 01),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false
        };

        // entry2: [02-20..null] -> must be soft-deleted as future
        var e2 = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = epId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeT,
            From = new DateOnly(2026, 02, 20),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false
        };

        await SeedEntryAsync(testDb, e1);
        await SeedEntryAsync(testDb, e2);

        await repo.CloseOnExcludeAsync(
            personId: personId,
            closeTo: closeTo,
            reason: "  test reason  ",
            author: "  admin  ",
            nowUtc: now.AddMinutes(1));

        await using var db = await testDb.Factory.CreateDbContextAsync();

        var ep = await db.TimeSheets.SingleAsync(x => x.Id == epId);
        Assert.Equal(closeTo, ep.ClosedAt);
        Assert.Equal("admin", ep.ClosedBy);
        Assert.Equal(now.AddMinutes(1), ep.ClosedAtUtc);

        var entry1 = await db.TimesheetEntries.SingleAsync(x => x.Id == e1.Id);
        Assert.Equal(closeTo, entry1.To);
        Assert.Equal("admin", entry1.UpdatedBy);
        Assert.Equal(now.AddMinutes(1), entry1.UpdatedAtUtc);
        Assert.False(entry1.IsDeleted);

        var entry2 = await db.TimesheetEntries.SingleAsync(x => x.Id == e2.Id);
        Assert.True(entry2.IsDeleted);
        Assert.Equal("admin", entry2.DeletedBy);
        Assert.Equal(now.AddMinutes(1), entry2.DeletedAtUtc);
        Assert.Equal("Auto-deleted: person excluded (test reason)", entry2.DeleteReason);
    }

    /// <summary>
    /// CloseOnExcludeAsync кидає, якщо closeTo раніше OpenedAt епізоду.
    /// </summary>
    [Fact]
    public async Task CloseOnExcludeAsync_Throws_WhenCloseToBeforeOpenedAt()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetEpisodeRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();

        // Allowed code exists + entry exists, але closeTo < openedAt => should throw
        var codeT = await SeedCodeAsync(testDb, TimesheetSystemCodes.BaseState);
        var epId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 10), null, "seed", now);

        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = epId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeT,
            From = new DateOnly(2026, 02, 10),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            IsDeleted = false
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CloseOnExcludeAsync(personId, new DateOnly(2026, 02, 09), null, "admin", now));

        Assert.Contains("він відкритий", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Схема БД гарантує, що для однієї особи може існувати тільки один активний епізод (ClosedAt == null).
    /// Це інваріанта даних, тому репозиторій не мусить (і не може) тестувати "два активних епізоди"
    /// через нормальний запис у БД.
    /// </summary>
    [Fact]
    public async Task Schema_EnforcesSingleActiveEpisodePerPerson()
    {
        await using var testDb = new SqliteTestDb();

        var now = Utc(2026, 02, 17, 10, 00);
        var personId = Guid.NewGuid();

        // First active episode
        await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), closedAt: null, "seed", now);

        // Second active episode for the same person must violate unique constraint
        await using var db = await testDb.Factory.CreateDbContextAsync();

        db.TimeSheets.Add(new TimeSheetAggregate
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            OpenedAt = new DateOnly(2026, 02, 02),
            ClosedAt = null,
            CreatedBy = "seed2",
            CreatedAtUtc = now.AddMinutes(1)
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static DateTime Utc(int y, int m, int d, int hh, int mm)
        => new(y, m, d, hh, mm, 0, DateTimeKind.Utc);

    /// <summary>
    /// Сідить TimesheetCodeDefinition (IsActive=true).
    /// </summary>
    private static async Task<Guid> SeedCodeAsync(SqliteTestDb testDb, string code)
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
            CreatedAtUtc = Utc(2026, 02, 17, 10, 00)
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
    /// Сідить епізод з одним TaskSpan (для перевірки Include в LoadEpisodeOnDateForUpdateAsync).
    /// </summary>
    private static async Task<Guid> SeedEpisodeWithSpanAsync(
        SqliteTestDb testDb,
        Guid personId,
        DateOnly openedAt,
        DateOnly spanFrom,
        DateTime nowUtc)
    {
        var ep = new TimeSheetAggregate
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            OpenedAt = openedAt,
            ClosedAt = null,
            CreatedBy = "seed",
            CreatedAtUtc = nowUtc
        };

        ep.UpsertTask(
            documentId: Guid.NewGuid(),
            missionId: Guid.NewGuid(),
            from: spanFrom,
            toExclusive: null,
            rnokpp: "0000000000",
            fullName: "P",
            rank: null,
            position: null,
            weapon: null,
            callsign: null,
            author: "seed",
            nowUtc: nowUtc);

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
