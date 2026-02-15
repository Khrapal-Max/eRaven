//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.MissionRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class MissionRepositoryTests
{
    private static readonly DateTime TodayLocal = new(2026, 02, 15, 10, 30, 00, DateTimeKind.Local);

    //======================================================================
    // Helpers
    //======================================================================

    private static Mission NewMission(
        string positionArea,
        string namePoint,
        string? typeDrone,
        string target,
        MissionMode mode,
        DateOnly createdAt,
        DateOnly? closedAt = null)
        => new()
        {
            Id = Guid.NewGuid(),
            PositionArea = positionArea,
            NamePoint = namePoint,
            TypeDrone = typeDrone,
            Target = target,
            MissionMode = mode,
            CreatedAt = createdAt,
            ClosedAt = closedAt
        };

    private static MissionAssignment NewAssignment(
        Guid documentId,
        Guid missionId,
        Guid personId,
        DateOnly from,
        DateOnly? to)
        => new()
        {
            Id = Guid.NewGuid(),
            CombatTaskDocumentId = documentId,
            MissionId = missionId,
            PersonId = personId,
            From = from,
            To = to,
            ClosedByDocumentId = null
        };

    //======================================================================
    // GetMissionsAsync
    //======================================================================

    [Fact]
    public async Task GetMissionsAsync_returns_all_missions()
    {
        await using var tdb = new SqliteTestDb();

        var m1 = NewMission("A", "P1", null, "T1", MissionMode.Day, new DateOnly(2026, 02, 01));
        var m2 = NewMission("B", "P2", "DJI", "T2", MissionMode.Night, new DateOnly(2026, 02, 02));

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.Missions.AddRange(m1, m2);
            await db.SaveChangesAsync();
        }

        var repo = new MissionRepository(tdb.Factory);

        var list = await repo.GetMissionsAsync();

        Assert.Equal(2, list.Count);
        Assert.Contains(list, x => x.Id == m1.Id);
        Assert.Contains(list, x => x.Id == m2.Id);
    }

    //======================================================================
    // AddMission
    //======================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddMission_throws_when_positionArea_invalid(string? positionArea)
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.AddMission(positionArea!, "P", "DJI", "Target", MissionMode.Day, TodayLocal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddMission_throws_when_target_invalid(string? target)
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.AddMission("Area", "P", "DJI", target!, MissionMode.Day, TodayLocal));
    }

    [Fact]
    public async Task AddMission_trims_fields_and_normalizes_optional_fields_and_sets_createdAt_from_todayLocal()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionRepository(tdb.Factory);

        var id = await repo.AddMission(
            positionArea: "  Area-1  ",
            namePoint: "   ",          // -> ""
            typeDrone: "  DJI  ",      // -> "DJI"
            target: "  Target-1 ",
            missionMode: MissionMode.Day,
            todayLocal: TodayLocal);

        Assert.NotEqual(Guid.Empty, id);

        await using var db = await tdb.Factory.CreateDbContextAsync();
        var mission = await db.Missions.AsNoTracking().SingleAsync(x => x.Id == id);

        Assert.Equal("Area-1", mission.PositionArea);
        Assert.Equal(string.Empty, mission.NamePoint);
        Assert.Equal("DJI", mission.TypeDrone);
        Assert.Equal("Target-1", mission.Target);
        Assert.Equal(MissionMode.Day, mission.MissionMode);
        Assert.Equal(DateOnly.FromDateTime(TodayLocal), mission.CreatedAt);
        Assert.Null(mission.ClosedAt);
    }

    [Fact]
    public async Task AddMission_sets_TypeDrone_null_when_whitespace()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionRepository(tdb.Factory);

        var id = await repo.AddMission(
            positionArea: "Area",
            namePoint: "P",
            typeDrone: "   ", // -> null
            target: "T",
            missionMode: MissionMode.Night,
            todayLocal: TodayLocal);

        await using var db = await tdb.Factory.CreateDbContextAsync();
        var mission = await db.Missions.AsNoTracking().SingleAsync(x => x.Id == id);

        Assert.Null(mission.TypeDrone);
    }

    //======================================================================
    // CloseMissionAsync
    //======================================================================

    [Fact]
    public async Task CloseMissionAsync_throws_when_mission_not_found()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CloseMissionAsync(Guid.NewGuid(), new DateOnly(2026, 02, 10)));

        Assert.Equal("Міссія не знайдена.", ex.Message);
    }

    [Fact]
    public async Task CloseMissionAsync_when_already_closed_should_noop()
    {
        await using var tdb = new SqliteTestDb();

        var mission = NewMission(
            positionArea: "A",
            namePoint: "P",
            typeDrone: null,
            target: "T",
            mode: MissionMode.Day,
            createdAt: new DateOnly(2026, 02, 01),
            closedAt: new DateOnly(2026, 02, 05));

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.Missions.Add(mission);
            await db.SaveChangesAsync();
        }

        var repo = new MissionRepository(tdb.Factory);

        await repo.CloseMissionAsync(mission.Id, closeAt: new DateOnly(2026, 02, 10));

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.Missions.AsNoTracking().SingleAsync(x => x.Id == mission.Id);

        Assert.Equal(new DateOnly(2026, 02, 05), reloaded.ClosedAt); // unchanged
    }

    [Fact]
    public async Task CloseMissionAsync_throws_when_closeAt_before_createdAt()
    {
        await using var tdb = new SqliteTestDb();

        var mission = NewMission("A", "P", null, "T", MissionMode.Day, createdAt: new DateOnly(2026, 02, 10));

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.Missions.Add(mission);
            await db.SaveChangesAsync();
        }

        var repo = new MissionRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CloseMissionAsync(mission.Id, closeAt: new DateOnly(2026, 02, 09)));

        Assert.Equal("Дата закриття не може бути раніше дати створення.", ex.Message);
    }

    [Fact]
    public async Task CloseMissionAsync_throws_when_has_active_assignments()
    {
        await using var tdb = new SqliteTestDb();

        var mission = NewMission("A", "P", null, "T", MissionMode.Day, createdAt: new DateOnly(2026, 02, 01));
        var assignment = NewAssignment(
            documentId: Guid.NewGuid(),
            missionId: mission.Id,
            personId: Guid.NewGuid(),
            from: new DateOnly(2026, 02, 05),
            to: null); // active

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.Missions.Add(mission);
            db.MissionAssignments.Add(assignment);
            await db.SaveChangesAsync();
        }

        var repo = new MissionRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CloseMissionAsync(mission.Id, closeAt: new DateOnly(2026, 02, 10)));

        Assert.Equal("Неможливо закрити міссію, оскільки вона має активні призначення.", ex.Message);
    }

    [Fact]
    public async Task CloseMissionAsync_allows_close_when_assignments_are_not_active()
    {
        await using var tdb = new SqliteTestDb();

        var mission = NewMission("A", "P", null, "T", MissionMode.Day, createdAt: new DateOnly(2026, 02, 01));

        // To != null => not active
        var assignment = NewAssignment(
            documentId: Guid.NewGuid(),
            missionId: mission.Id,
            personId: Guid.NewGuid(),
            from: new DateOnly(2026, 02, 05),
            to: new DateOnly(2026, 02, 06));

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.Missions.Add(mission);
            db.MissionAssignments.Add(assignment);
            await db.SaveChangesAsync();
        }

        var repo = new MissionRepository(tdb.Factory);

        await repo.CloseMissionAsync(mission.Id, closeAt: new DateOnly(2026, 02, 10));

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.Missions.AsNoTracking().SingleAsync(x => x.Id == mission.Id);

        Assert.Equal(new DateOnly(2026, 02, 10), reloaded.ClosedAt);
    }
}
