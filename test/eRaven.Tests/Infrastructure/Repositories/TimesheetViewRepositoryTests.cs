//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetViewRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

/// <summary>
/// Тести для <see cref="TimesheetViewRepository"/>.
///
/// <para>
/// Фіксуємо контракт read-side проєкції “інтервали в БД → матриця по днях”:
/// <list type="bullet">
/// <item><description>Entry інтервали зберігаються як <c>[From..To)</c> (<c>To</c> — <b>exclusive</b>).</description></item>
/// <item><description>Епізод має межі <see cref="TimeSheetAggregate.OpenedAt"/> та <see cref="TimeSheetAggregate.ClosedAt"/> (ClosedAt — inclusive).</description></item>
/// <item><description>Матриця по днях будується в памʼяті та заповнює “дірки” системним станом <c>НБ</c>.</description></item>
/// <item><description>Soft-deleted entries (<c>IsDeleted=true</c>) ігноруються.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class TimesheetViewRepositoryTests
{
    //======================================================================
    // Validation
    //======================================================================

    /// <summary>
    /// Перевіряємо валідацію параметрів (рік/місяць/personId/діапазон).
    /// </summary>
    [Fact]
    public async Task Validation_Throws_OnInvalidArguments()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetViewRepository(testDb.Factory);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repo.GetTimesheetsMonthAsync(1999, 1));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repo.GetTimesheetsMonthAsync(2101, 1));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repo.GetTimesheetsMonthAsync(2026, 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repo.GetTimesheetsMonthAsync(2026, 13));

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetTimesheetPersonMonthAsync(Guid.Empty, 2026, 2));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repo.GetTimesheetPersonMonthAsync(Guid.NewGuid(), 1999, 2));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repo.GetTimesheetPersonMonthAsync(Guid.NewGuid(), 2026, 0));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repo.GetTimesheetsRangeAsync(new DateOnly(2026, 02, 10), new DateOnly(2026, 02, 09)));
    }

    //======================================================================
    // Person month
    //======================================================================

    /// <summary>
    /// Якщо у вказаної людини немає епізодів, метод повертає null.
    /// </summary>
    [Fact]
    public async Task GetTimesheetPersonMonthAsync_ReturnsNull_WhenNoEpisodesForPerson()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetViewRepository(testDb.Factory);

        await SeedCodeAsync(testDb, TimesheetSystemCodes.NotInTimesheet);
        await SeedCodeAsync(testDb, TimesheetSystemCodes.BaseState);

        // episode for another person
        await SeedEpisodeAsync(testDb, Guid.NewGuid(), new DateOnly(2026, 02, 01), null, "seed", Utc(2026, 02, 01, 10, 00));

        var personId = Guid.NewGuid();
        var rm = await repo.GetTimesheetPersonMonthAsync(personId, 2026, 2);

        Assert.Null(rm);
    }

    //======================================================================
    // Range projection
    //======================================================================

    /// <summary>
    /// Матриця має бути “підрізана” межами епізоду:
    /// якщо запит ширший, повертаємо лише дні в межах [OpenedAt..ClosedAt] (ClosedAt inclusive).
    /// </summary>
    [Fact]
    public async Task GetTimesheetsRangeAsync_ClampsToEpisodeBounds()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetViewRepository(testDb.Factory);

        await SeedCodeAsync(testDb, TimesheetSystemCodes.NotInTimesheet);
        var baseId = await SeedCodeAsync(testDb, TimesheetSystemCodes.BaseState);

        var personId = Guid.NewGuid();
        var epId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 03), new DateOnly(2026, 02, 05), "seed", Utc(2026, 02, 01, 10, 00));

        // base entry open-ended for the episode start
        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = epId,
            PersonId = personId,
            TimesheetCodeDefinitionId = baseId,
            From = new DateOnly(2026, 02, 03),
            To = null,
            Reference = null,
            Note = null,
            CreatedBy = "seed",
            CreatedAtUtc = Utc(2026, 02, 01, 10, 00),
            IsDeleted = false
        });

        var periods = await repo.GetTimesheetsRangeAsync(
            fromDate: new DateOnly(2026, 02, 01),
            toDate: new DateOnly(2026, 02, 10));

        var p = periods.Single(x => x.PersonId == personId);

        // episode: 03,04,05 => 3 дні
        Assert.Equal(3, p.Days.Count);
        Assert.Equal(new DateOnly(2026, 02, 03), p.Days[0].DateOfDay);
        Assert.Equal(new DateOnly(2026, 02, 05), p.Days[^1].DateOfDay);
    }

    /// <summary>
    /// Перевіряємо half-open семантику переходу на межі:
    /// entry1 активний на 09.02, але вже не активний на 10.02 (бо To=10.02 exclusive),
    /// а з 10.02 активний entry2 (From=10.02).
    /// </summary>
    [Fact]
    public async Task GetTimesheetsRangeAsync_SwitchesOnBoundary_ToIsExclusive()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetViewRepository(testDb.Factory);

        await SeedCodeAsync(testDb, TimesheetSystemCodes.NotInTimesheet);
        var code30 = await SeedCodeAsync(testDb, TimesheetSystemCodes.ReadyToCombatTask);
        var codeT = await SeedCodeAsync(testDb, TimesheetSystemCodes.BaseState);

        var personId = Guid.NewGuid();
        var epId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", Utc(2026, 02, 01, 10, 00));

        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = epId,
            PersonId = personId,
            TimesheetCodeDefinitionId = code30,
            From = new DateOnly(2026, 02, 01),
            To = new DateOnly(2026, 02, 10), // exclusive
            Reference = "doc-30",
            Note = "n1",
            CreatedBy = "seed",
            CreatedAtUtc = Utc(2026, 02, 01, 10, 00),
            IsDeleted = false
        });

        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = epId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeT,
            From = new DateOnly(2026, 02, 10),
            To = null,
            Reference = null,
            Note = "n2",
            CreatedBy = "seed",
            CreatedAtUtc = Utc(2026, 02, 01, 10, 00),
            IsDeleted = false
        });

        var periods = await repo.GetTimesheetsRangeAsync(
            fromDate: new DateOnly(2026, 02, 09),
            toDate: new DateOnly(2026, 02, 10)); // inclusive; toExclusive=11

        var p = periods.Single(x => x.PersonId == personId);

        Assert.Equal(2, p.Days.Count);

        Assert.Equal(new DateOnly(2026, 02, 09), p.Days[0].DateOfDay);
        Assert.Equal(TimesheetSystemCodes.ReadyToCombatTask, p.Days[0].Code);
        Assert.Equal("doc-30", p.Days[0].Reference);
        Assert.Equal("n1", p.Days[0].Note);

        Assert.Equal(new DateOnly(2026, 02, 10), p.Days[1].DateOfDay);
        Assert.Equal(TimesheetSystemCodes.BaseState, p.Days[1].Code);
        Assert.Null(p.Days[1].Reference);
        Assert.Equal("n2", p.Days[1].Note);
    }

    /// <summary>
    /// Якщо між інтервалами є “дірка” — матриця заповнює її системним станом <c>НБ</c>.
    /// </summary>
    [Fact]
    public async Task GetTimesheetsRangeAsync_FillsGapsWithNb()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetViewRepository(testDb.Factory);

        var nbId = await SeedCodeAsync(testDb, TimesheetSystemCodes.NotInTimesheet);
        var code30 = await SeedCodeAsync(testDb, TimesheetSystemCodes.ReadyToCombatTask);

        var personId = Guid.NewGuid();
        var epId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", Utc(2026, 02, 01, 10, 00));

        // [01..05)
        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = epId,
            PersonId = personId,
            TimesheetCodeDefinitionId = code30,
            From = new DateOnly(2026, 02, 01),
            To = new DateOnly(2026, 02, 05),
            Reference = null,
            Note = null,
            CreatedBy = "seed",
            CreatedAtUtc = Utc(2026, 02, 01, 10, 00),
            IsDeleted = false
        });

        // gap on 05-06; next starts 07
        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = epId,
            PersonId = personId,
            TimesheetCodeDefinitionId = code30,
            From = new DateOnly(2026, 02, 07),
            To = null,
            Reference = null,
            Note = null,
            CreatedBy = "seed",
            CreatedAtUtc = Utc(2026, 02, 01, 10, 00),
            IsDeleted = false
        });

        var periods = await repo.GetTimesheetsRangeAsync(
            fromDate: new DateOnly(2026, 02, 04),
            toDate: new DateOnly(2026, 02, 08));

        var p = periods.Single(x => x.PersonId == personId);

        // days: 04,05,06,07,08
        Assert.Equal(5, p.Days.Count);

        Assert.Equal(new DateOnly(2026, 02, 04), p.Days[0].DateOfDay);
        Assert.Equal(TimesheetSystemCodes.ReadyToCombatTask, p.Days[0].Code);

        Assert.Equal(new DateOnly(2026, 02, 05), p.Days[1].DateOfDay);
        Assert.Equal(TimesheetSystemCodes.NotInTimesheet, p.Days[1].Code);
        Assert.Equal(nbId, p.Days[1].CodeId);
        Assert.Equal(Guid.Empty, p.Days[1].TimesheetId);

        Assert.Equal(new DateOnly(2026, 02, 06), p.Days[2].DateOfDay);
        Assert.Equal(TimesheetSystemCodes.NotInTimesheet, p.Days[2].Code);

        Assert.Equal(new DateOnly(2026, 02, 07), p.Days[3].DateOfDay);
        Assert.Equal(TimesheetSystemCodes.ReadyToCombatTask, p.Days[3].Code);

        Assert.Equal(new DateOnly(2026, 02, 08), p.Days[4].DateOfDay);
        Assert.Equal(TimesheetSystemCodes.ReadyToCombatTask, p.Days[4].Code);
    }

    /// <summary>
    /// Soft-deleted entries не повинні впливати на матрицю (в результаті буде НБ).
    /// </summary>
    [Fact]
    public async Task GetTimesheetsDayAsync_IgnoresSoftDeletedEntries()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetViewRepository(testDb.Factory);

        var nbId = await SeedCodeAsync(testDb, TimesheetSystemCodes.NotInTimesheet);
        var code30 = await SeedCodeAsync(testDb, TimesheetSystemCodes.ReadyToCombatTask);

        var personId = Guid.NewGuid();
        var epId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), null, "seed", Utc(2026, 02, 01, 10, 00));

        await SeedEntryAsync(testDb, new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimesheetId = epId,
            PersonId = personId,
            TimesheetCodeDefinitionId = code30,
            From = new DateOnly(2026, 02, 01),
            To = null,
            Reference = null,
            Note = null,
            CreatedBy = "seed",
            CreatedAtUtc = Utc(2026, 02, 01, 10, 00),
            IsDeleted = true,
            DeletedBy = "seed",
            DeletedAtUtc = Utc(2026, 02, 01, 11, 00),
            DeleteReason = "test"
        });

        var periods = await repo.GetTimesheetsDayAsync(new DateOnly(2026, 02, 01));
        var p = periods.Single(x => x.PersonId == personId);

        Assert.Single(p.Days);
        Assert.Equal(TimesheetSystemCodes.NotInTimesheet, p.Days[0].Code);
        Assert.Equal(nbId, p.Days[0].CodeId);
        Assert.Equal(Guid.Empty, p.Days[0].TimesheetId);
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static DateTime Utc(int y, int m, int d, int hh = 0, int mm = 0)
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
            CreatedAtUtc = Utc(2026, 02, 01, 10, 00)
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
