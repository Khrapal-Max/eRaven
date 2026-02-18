//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.MissionRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

/// <summary>
/// Тести для <see cref="MissionRepository"/>.
///
/// <para>
/// Стратегія:
/// <list type="bullet">
/// <item><description>місію можна закрити лише якщо немає активного призначення (TimesheetTaskSpan) на дату закриття;</description></item>
/// <item><description>активність визначається як <c>Status != Canceled</c> та half-open інтервал <c>[FromDate..ToDate)</c>;</description></item>
/// <item><description>canceled-спани не блокують закриття місії.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class MissionRepositoryTests
{
    //======================================================================
    // AddMission
    //======================================================================

    /// <summary>
    /// AddMission створює місію з trim полями, CreatedAt з todayLocal,
    /// та не підміняє відсутній NamePoint на "" (зберігає null).
    /// </summary>
    [Fact]
    public async Task AddMission_PersistsTrim_AndNullNamePoint_AndCreatedAt()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        var todayLocal = new DateTime(2026, 02, 17, 23, 59, 00, DateTimeKind.Unspecified);

        var id = await repo.AddMission(
            positionArea: "  Area-1  ",
            namePoint: "   ",
            typeDrone: "  UAS  ",
            target: "  Target  ",
            missionMode: MissionMode.Day,
            todayLocal: todayLocal);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        var m = await db.Missions.SingleAsync(x => x.Id == id);

        Assert.Equal("Area-1", m.PositionArea);
        Assert.Null(m.NamePoint);
        Assert.Equal("UAS", m.TypeDrone);
        Assert.Equal("Target", m.Target);
        Assert.Equal(MissionMode.Day, m.MissionMode);
        Assert.Equal(DateOnly.FromDateTime(todayLocal), m.CreatedAt);
        Assert.Null(m.ClosedAt);
    }

    /// <summary>
    /// AddMission валідовує обов'язкові поля.
    /// </summary>
    [Fact]
    public async Task AddMission_Throws_OnInvalidArguments()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.AddMission(" ", null, null, "T", MissionMode.Day, DateTime.Today));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.AddMission("A", null, null, "   ", MissionMode.Day, DateTime.Today));
    }

    //======================================================================
    // CloseMissionAsync (strategy)
    //======================================================================

    /// <summary>
    /// CloseMissionAsync закриває місію, якщо активних призначень немає.
    /// </summary>
    [Fact]
    public async Task CloseMissionAsync_SetsClosedAt_WhenNoActiveAssignments()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        var today = new DateTime(2026, 02, 01, 12, 00, 00);
        var id = await repo.AddMission("Area", "P", null, "T", MissionMode.Day, today);

        await repo.CloseMissionAsync(id, closeAt: new DateOnly(2026, 02, 10));

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var m = await db.Missions.SingleAsync(x => x.Id == id);

        Assert.Equal(new DateOnly(2026, 02, 10), m.ClosedAt);
    }

    /// <summary>
    /// CloseMissionAsync — ідемпотентний для вже закритої місії.
    /// </summary>
    [Fact]
    public async Task CloseMissionAsync_IsIdempotent_WhenAlreadyClosed()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        var id = await repo.AddMission("Area", null, null, "T", MissionMode.Day, new DateTime(2026, 02, 01));

        await repo.CloseMissionAsync(id, new DateOnly(2026, 02, 10));
        await repo.CloseMissionAsync(id, new DateOnly(2026, 02, 11)); // ignored

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var m = await db.Missions.SingleAsync(x => x.Id == id);

        Assert.Equal(new DateOnly(2026, 02, 10), m.ClosedAt);
    }

    /// <summary>
    /// CloseMissionAsync кидає помилку, якщо closeAt раніше CreatedAt.
    /// </summary>
    [Fact]
    public async Task CloseMissionAsync_Throws_WhenCloseAtBeforeCreatedAt()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        var id = await repo.AddMission("Area", null, null, "T", MissionMode.Day, new DateTime(2026, 02, 10));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CloseMissionAsync(id, closeAt: new DateOnly(2026, 02, 01)));
    }

    /// <summary>
    /// CloseMissionAsync забороняє закривати місію, якщо існує активний TaskSpan на дату закриття.
    /// </summary>
    [Fact]
    public async Task CloseMissionAsync_Throws_WhenHasActiveTaskSpanOnCloseDate()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        var missionId = await repo.AddMission("Area", null, null, "T", MissionMode.Day, new DateTime(2026, 02, 01));

        // Active assignment on 2026-02-10: [2026-02-09 .. null)
        await SeedTaskSpanAsync(
            testDb,
            missionId: missionId,
            from: new DateOnly(2026, 02, 09),
            toExclusive: null,
            statusCanceled: false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CloseMissionAsync(missionId, closeAt: new DateOnly(2026, 02, 10)));

        Assert.Contains("активні призначення", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// CloseMissionAsync НЕ блокує закриття місії, якщо призначення є, але воно Canceled.
    /// (Cancel — компенсація, факт неактивний.)
    /// </summary>
    [Fact]
    public async Task CloseMissionAsync_AllowsClose_WhenOnlyCanceledSpanExists()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        var missionId = await repo.AddMission("Area", null, null, "T", MissionMode.Day, new DateTime(2026, 02, 01));

        // Canceled span, ToDate null (компенсація) — не повинен блокувати
        await SeedTaskSpanAsync(
            testDb,
            missionId: missionId,
            from: new DateOnly(2026, 02, 09),
            toExclusive: null,
            statusCanceled: true);

        await repo.CloseMissionAsync(missionId, closeAt: new DateOnly(2026, 02, 10));

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var m = await db.Missions.SingleAsync(x => x.Id == missionId);

        Assert.Equal(new DateOnly(2026, 02, 10), m.ClosedAt);
    }

    /// <summary>
    /// CloseMissionAsync дозволяє закриття, якщо span завершився до closeAt за half-open правилом.
    /// Напр.: ToDate == closeAt означає, що на closeAt він вже неактивний.
    /// </summary>
    [Fact]
    public async Task CloseMissionAsync_AllowsClose_WhenSpanEndedBeforeCloseAtExclusive()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        var missionId = await repo.AddMission("Area", null, null, "T", MissionMode.Day, new DateTime(2026, 02, 01));

        // Span: [2026-02-08 .. 2026-02-10) => NOT active on 2026-02-10
        await SeedTaskSpanAsync(
            testDb,
            missionId: missionId,
            from: new DateOnly(2026, 02, 08),
            toExclusive: new DateOnly(2026, 02, 10),
            statusCanceled: false);

        await repo.CloseMissionAsync(missionId, closeAt: new DateOnly(2026, 02, 10));

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var m = await db.Missions.SingleAsync(x => x.Id == missionId);

        Assert.Equal(new DateOnly(2026, 02, 10), m.ClosedAt);
    }

    //======================================================================
    // Helpers
    //======================================================================

    /// <summary>
    /// Сідить епізод табеля з одним TaskSpan для вказаної місії.
    /// Використовує доменний агрегат, щоб зберегти інваріанти та required поля.
    /// </summary>
    private static async Task SeedTaskSpanAsync(
        SqliteTestDb testDb,
        Guid missionId,
        DateOnly from,
        DateOnly? toExclusive,
        bool statusCanceled)
    {
        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var ep = new TimeSheetAggregate
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            OpenedAt = new DateOnly(2026, 02, 01),
            ClosedAt = null,
            CreatedBy = "seed",
            CreatedAtUtc = now
        };

        var documentId = Guid.NewGuid();

        ep.UpsertTask(
            documentId: documentId,
            missionId: missionId,
            from: from,
            toExclusive: toExclusive,
            rnokpp: "1234567890",
            fullName: "Test Person",
            rank: null,
            position: null,
            weapon: null,
            callsign: null,
            openedByDocumentReference: "DOC1",
            author: "seed",
            nowUtc: now);

        if (statusCanceled)
        {
            ep.CancelTask(
                documentId: documentId,
                missionId: missionId,
                reasonCodeId: Guid.NewGuid(),
                reference: "VOID",
                author: "seed",
                nowUtc: now.AddMinutes(1));
        }

        await using var db = await testDb.Factory.CreateDbContextAsync();
        db.TimeSheets.Add(ep);
        await db.SaveChangesAsync();
    }
}
