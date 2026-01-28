//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.MissionRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class MissionRepositoryTests
{
    [Fact(DisplayName = "MissionRepo: AddMissionPoint створює місію, trim полів + CreatedAt з todayLocal")]
    public async Task AddMissionPoint_CreatesMission_Trims_AndSetsCreatedAt()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        var todayLocal = new DateTime(2026, 01, 10, 12, 30, 00, DateTimeKind.Local);
        var typeDrone = "DJI";
        var target = "Розвідка";

        var id = await repo.AddMissionPoint(
            positionArea: "  Район-1  ",
            namePoint: "  Точка-А  ",
            typeDrone: typeDrone,
            target: target,
            missionMode: MissionMode.Day,
            todayLocal: todayLocal);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var m = await db.Missions.SingleAsync(x => x.Id == id);

        Assert.Equal("Район-1", m.PositionArea);
        Assert.Equal("Точка-А", m.NamePoint);
        Assert.Equal(MissionMode.Day, m.MissionMode);
        Assert.Equal(DateOnly.FromDateTime(todayLocal), m.CreatedAt);
        Assert.Null(m.ClosedAt);

        Assert.NotNull(m.TypeDrone);
        Assert.Equal("DJI", m.TypeDrone);

        Assert.NotNull(m.Target);
        Assert.Equal("Розвідка", m.Target);
    }

    [Fact(DisplayName = "MissionRepo: AddMissionPoint коли namePoint пустий/пробіли — зберігає '')")]
    public async Task AddMissionPoint_WhitespaceNamePoint_SavesNull()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        var id = await repo.AddMissionPoint(
            positionArea: "Район-1",
            namePoint: "   ",
            typeDrone: null,
            target: "Охорона",
            missionMode: MissionMode.Night,
            todayLocal: new DateTime(2026, 01, 10));

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var m = await db.Missions.SingleAsync(x => x.Id == id);

        Assert.Equal("Район-1", m.PositionArea);
        Assert.Equal("", m.NamePoint);
        Assert.Equal(MissionMode.Night, m.MissionMode);
        Assert.Null(m.TypeDrone);
        Assert.Equal("Охорона", m.Target);
    }

    [Fact(DisplayName = "MissionRepo: GetMissionPointsAsync повертає всі місії")]
    public async Task GetMissionPointsAsync_ReturnsAll()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        await repo.AddMissionPoint(
            positionArea: "A",
            namePoint: "P1",
            typeDrone: null,
            target: "T1",
            missionMode: MissionMode.Day,
            todayLocal: new DateTime(2026, 01, 01));

        await repo.AddMissionPoint(
            positionArea: "B",
            namePoint: null,
            typeDrone: "N",
            target: "T2",
            missionMode: MissionMode.FullTime,
            todayLocal: new DateTime(2026, 01, 02));

        var all = await repo.GetMissionPointsAsync();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, x => x.PositionArea == "A" && x.NamePoint == "P1");
        Assert.Contains(all, x => x.PositionArea == "B");
    }

    [Fact(DisplayName = "MissionRepo: CloseMissionPointAsync ставить ClosedAt, повторне закриття — ідемпотентне")]
    public async Task CloseMissionPointAsync_SetsClosedAt_AndIsIdempotent()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        var todayLocal = new DateTime(2026, 01, 05);
        var id = await repo.AddMissionPoint(
            positionArea: "Район-1",
            namePoint: "Точка",
            typeDrone: null,
            target: "T",
            missionMode: MissionMode.Day,
            todayLocal: todayLocal);

        var closeAt = new DateOnly(2026, 01, 10);

        await repo.CloseMissionPointAsync(id, closeAt);
        await repo.CloseMissionPointAsync(id, closeAt); // вдруге — не має падати

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var m = await db.Missions.SingleAsync(x => x.Id == id);

        Assert.Equal(closeAt, m.ClosedAt);
    }

    [Fact(DisplayName = "MissionRepo: CloseMissionPointAsync кидає помилку, якщо closeAt < CreatedAt")]
    public async Task CloseMissionPointAsync_WhenCloseBeforeCreate_Throws()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        var todayLocal = new DateTime(2026, 01, 10);
        var id = await repo.AddMissionPoint(
            positionArea: "Район-1",
            namePoint: null,
            typeDrone: null,
            target: "T",
            missionMode: MissionMode.Day,
            todayLocal: todayLocal);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repo.CloseMissionPointAsync(id, new DateOnly(2026, 01, 09)));

        Assert.Contains("Дата закриття не може бути раніше дати створення", ex.Message);
    }

    [Fact(DisplayName = "MissionRepo: CloseMissionPointAsync кидає помилку, якщо місію не знайдено")]
    public async Task CloseMissionPointAsync_WhenNotFound_Throws()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repo.CloseMissionPointAsync(Guid.NewGuid(), new DateOnly(2026, 01, 01)));

        Assert.Contains("Міссія не знайдена", ex.Message);
    }

    [Fact(DisplayName = "MissionRepo: AddMissionPoint кидає помилку, якщо PositionArea порожній")]
    public async Task AddMissionPoint_WhenPositionAreaEmpty_Throws()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.AddMissionPoint(
            positionArea: "  ",
            namePoint: null,
            typeDrone: null,
            target: "T",
            missionMode: MissionMode.Day,
            todayLocal: new DateTime(2026, 01, 01)));

        Assert.Equal("positionArea", ex.ParamName);
    }

    [Fact(DisplayName = "MissionRepo: друга відкрита місія з тим самим ключем — DbUpdateException (унікальний індекс)")]
    public async Task AddMissionPoint_DuplicateOpen_Throws()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        var today = new DateTime(2026, 01, 10);

        // 1-ша (відкрита)
        await repo.AddMissionPoint(
            positionArea: "A",
            namePoint: "P",
            typeDrone: "DJI",
            target: "T",
            missionMode: MissionMode.Day,
            todayLocal: today);

        // 2-га (та сама комбінація, теж відкрита) -> має впасти
        await Assert.ThrowsAsync<DbUpdateException>(() => repo.AddMissionPoint(
            positionArea: "A",
            namePoint: "P",
            typeDrone: "ANY",
            target: "T",
            missionMode: MissionMode.Day,
            todayLocal: today));
    }

    [Fact(DisplayName = "MissionRepo: після закриття можна створити таку ж місію повторно")]
    public async Task AddMissionPoint_AfterClose_AllowsSameKeyAgain()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new MissionRepository(testDb.Factory);

        var today = new DateTime(2026, 01, 10);
        var id = await repo.AddMissionPoint(
            positionArea: "A",
            namePoint: "P",
            typeDrone: null,
            target: "T",
            missionMode: MissionMode.Day,
            todayLocal: today);

        await repo.CloseMissionPointAsync(id, new DateOnly(2026, 01, 11));

        // тепер такий самий ключ — має пройти
        var id2 = await repo.AddMissionPoint(
            positionArea: "A",
            namePoint: "P",
            typeDrone: null,
            target: "T",
            missionMode: MissionMode.Day,
            todayLocal: today);

        Assert.NotEqual(id, id2);
    }
}
