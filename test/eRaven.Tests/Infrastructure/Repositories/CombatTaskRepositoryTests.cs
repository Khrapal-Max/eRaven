//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class CombatTaskRepositoryTests
{
    [Fact]
    public async Task CreateCombatTaskAsync_CreatesTaskAndDetails_TrimsSourceDocument()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskRepository(testDb.Factory);

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        await SeedActiveDocumentAsync(testDb, docId);
        await SeedMissionAsync(testDb, missionId);

        var day = new DateOnly(2026, 02, 01);

        var d1 = NewDetails(Guid.NewGuid(), Guid.NewGuid(), CombatTaskDetailsKind.Start, day, "1111111111", "Петро Петренко");
        var d2 = NewDetails(Guid.NewGuid(), Guid.NewGuid(), CombatTaskDetailsKind.End, day.AddDays(3), "2222222222", "Іван Іваненко");

        var taskId = await repo.CreateCombatTaskAsync(
            documentId: docId,
            missionId: missionId,
            sourceDocument: "  Наказ №1  ",
            combatTaskDetails: [d1, d2]);

        await using var db = testDb.Factory.CreateDbContext();

        var task = await db.CombatTasks
            .Include(x => x.CombatTaskDetails)
            .SingleAsync(x => x.Id == taskId);

        Assert.Equal(docId, task.CombatTaskDocumentId);
        Assert.Equal(missionId, task.MissionId);
        Assert.Equal("Наказ №1", task.SourceDocument);
        Assert.Equal(2, task.CombatTaskDetails.Count);

        Assert.Contains(task.CombatTaskDetails, x => x.Id == d1.Id && x.CombatTaskId == taskId);
        Assert.Contains(task.CombatTaskDetails, x => x.Id == d2.Id && x.CombatTaskId == taskId);
    }

    [Fact]
    public async Task CreateCombatTaskAsync_Throws_WhenTaskForMissionAlreadyExists()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskRepository(testDb.Factory);

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        await SeedActiveDocumentAsync(testDb, docId);
        await SeedMissionAsync(testDb, missionId);

        var day = new DateOnly(2026, 02, 01);
        var d1 = NewDetails(Guid.NewGuid(), Guid.NewGuid(), CombatTaskDetailsKind.Start, day, "1111111111", "Петро Петренко");

        _ = await repo.CreateCombatTaskAsync(docId, missionId, "Наказ №1", [d1]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CreateCombatTaskAsync(docId, missionId, "Наказ №1", [d1]));

        Assert.Contains("вже існує", ex.Message);
    }

    [Fact]
    public async Task CreateCombatTaskAsync_Throws_WhenDocumentCanceled()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskRepository(testDb.Factory);

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        await SeedDocumentAsync(testDb, docId, DocumentStatus.Canceled);
        await SeedMissionAsync(testDb, missionId);

        var day = new DateOnly(2026, 02, 01);
        var d1 = NewDetails(Guid.NewGuid(), Guid.NewGuid(), CombatTaskDetailsKind.Start, day, "1111111111", "Петро Петренко");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CreateCombatTaskAsync(docId, missionId, "Наказ №1", [d1]));

        Assert.Contains("Документ скасовано", ex.Message);
    }

    [Fact]
    public async Task UpsertCombatTaskAsync_CreatesWhenMissing()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskRepository(testDb.Factory);

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        await SeedActiveDocumentAsync(testDb, docId);
        await SeedMissionAsync(testDb, missionId);

        var day = new DateOnly(2026, 02, 05);
        var d1 = NewDetails(Guid.NewGuid(), Guid.NewGuid(), CombatTaskDetailsKind.Start, day, "1111111111", "Петро Петренко");

        var taskId = await repo.UpsertCombatTaskAsync(docId, missionId, "  Наказ №2 ", [d1]);

        await using var db = testDb.Factory.CreateDbContext();

        var task = await db.CombatTasks
            .Include(x => x.CombatTaskDetails)
            .SingleAsync(x => x.Id == taskId);

        Assert.Equal("Наказ №2", task.SourceDocument);
        Assert.Single(task.CombatTaskDetails);
        Assert.Equal(d1.Id, task.CombatTaskDetails.Single().Id);
    }

    [Fact]
    public async Task UpsertCombatTaskAsync_UpdatesExisting_AndSyncsDetailsByStableId()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskRepository(testDb.Factory);

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        await SeedActiveDocumentAsync(testDb, docId);
        await SeedMissionAsync(testDb, missionId);

        var day = new DateOnly(2026, 02, 10);

        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var cId = Guid.NewGuid();

        var personA = Guid.NewGuid();
        var personB = Guid.NewGuid();
        var personC = Guid.NewGuid();

        var a1 = NewDetails(aId, personA, CombatTaskDetailsKind.Start, day, "1111111111", "Петро Петренко");
        var b1 = NewDetails(bId, personB, CombatTaskDetailsKind.Start, day, "2222222222", "Іван Іваненко");

        var taskId = await repo.UpsertCombatTaskAsync(docId, missionId, "Наказ №3", [a1, b1]);

        // Update A (same Id), remove B, add C
        var a2 = NewDetails(aId, personA, CombatTaskDetailsKind.End, day.AddDays(1), "1111111111", "Петро Петренко (upd)");
        var c1 = NewDetails(cId, personC, CombatTaskDetailsKind.Start, day.AddDays(2), "3333333333", "Олег Олегович");

        var taskId2 = await repo.UpsertCombatTaskAsync(docId, missionId, "  Наказ №3 (upd)  ", [a2, c1]);

        Assert.Equal(taskId, taskId2);

        await using var db = testDb.Factory.CreateDbContext();

        var task = await db.CombatTasks
            .Include(x => x.CombatTaskDetails)
            .SingleAsync(x => x.Id == taskId);

        Assert.Equal("Наказ №3 (upd)", task.SourceDocument);
        Assert.Equal(2, task.CombatTaskDetails.Count);

        var a = task.CombatTaskDetails.Single(x => x.Id == aId);
        Assert.Equal(CombatTaskDetailsKind.End, a.Kind);
        Assert.Equal(day.AddDays(1), a.EffectiveAt);
        Assert.Equal("Петро Петренко (upd)", a.FullName);

        Assert.DoesNotContain(task.CombatTaskDetails, x => x.Id == bId);

        var c = task.CombatTaskDetails.Single(x => x.Id == cId);
        Assert.Equal(personC, c.PersonId);
    }

    [Fact]
    public async Task UpsertCombatTaskAsync_Throws_WhenDuplicateDetailsIdInRequest()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskRepository(testDb.Factory);

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        await SeedActiveDocumentAsync(testDb, docId);
        await SeedMissionAsync(testDb, missionId);

        var day = new DateOnly(2026, 02, 01);
        var id = Guid.NewGuid();

        var d1 = NewDetails(id, Guid.NewGuid(), CombatTaskDetailsKind.Start, day, "1111111111", "A");
        var d2 = NewDetails(id, Guid.NewGuid(), CombatTaskDetailsKind.End, day, "2222222222", "B"); // duplicate Id

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.UpsertCombatTaskAsync(docId, missionId, "Наказ", [d1, d2]));

        Assert.Contains("unique", ex.Message);
    }

    [Fact]
    public async Task DeleteCombatTaskAsync_RemovesTask_IsIdempotent()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskRepository(testDb.Factory);

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        await SeedActiveDocumentAsync(testDb, docId);
        await SeedMissionAsync(testDb, missionId);

        var day = new DateOnly(2026, 02, 01);
        var d1 = NewDetails(Guid.NewGuid(), Guid.NewGuid(), CombatTaskDetailsKind.Start, day, "1111111111", "A");

        var taskId = await repo.UpsertCombatTaskAsync(docId, missionId, "Наказ", [d1]);

        await repo.DeleteCombatTaskAsync(docId, taskId);
        await repo.DeleteCombatTaskAsync(docId, taskId); // idempotent

        await using var db = testDb.Factory.CreateDbContext();

        var exists = await db.CombatTasks
            .AsNoTracking()
            .AnyAsync(x => x.Id == taskId);

        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteCombatTaskAsync_Throws_WhenDocumentCanceled()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskRepository(testDb.Factory);

        var docId = Guid.NewGuid();
        await SeedDocumentAsync(testDb, docId, DocumentStatus.Canceled);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.DeleteCombatTaskAsync(docId, Guid.NewGuid()));

        Assert.Contains("Документ скасовано", ex.Message);
    }

    [Fact]
    public async Task GetDocumentAsync_ReturnsDocument_WithTasksAndDetails()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskRepository(testDb.Factory);

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        await SeedActiveDocumentAsync(testDb, docId);
        await SeedMissionAsync(testDb, missionId);

        var day = new DateOnly(2026, 02, 01);
        var d1 = NewDetails(Guid.NewGuid(), Guid.NewGuid(), CombatTaskDetailsKind.Start, day, "1111111111", "A");
        var d2 = NewDetails(Guid.NewGuid(), Guid.NewGuid(), CombatTaskDetailsKind.End, day.AddDays(1), "2222222222", "B");

        _ = await repo.CreateCombatTaskAsync(docId, missionId, "Наказ", [d1, d2]);

        var doc = await repo.GetDocumentAsync(docId);

        Assert.Equal(docId, doc.Id);
        Assert.Single(doc.CombatTasks);
        Assert.Equal(2, doc.CombatTasks.Single().CombatTaskDetails.Count);
    }

    [Fact]
    public async Task GetDocumentAsync_Throws_WhenNotFound()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskRepository(testDb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.GetDocumentAsync(Guid.NewGuid()));

        Assert.Contains("не знайдено", ex.Message);
    }

    [Fact]
    public async Task GetDocumentMissionIdsAsync_Throws_WhenDocumentIdEmpty()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskRepository(testDb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetDocumentMissionIdsAsync(Guid.Empty));
    }

    [Fact]
    public async Task GetDocumentMissionIdsAsync_ReturnsEmpty_WhenNoTasks()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskRepository(testDb.Factory);

        var documentId = Guid.NewGuid();

        // NOTE: метод читає лише combat_tasks, тому навіть без документа очікуємо empty.
        var ids = await repo.GetDocumentMissionIdsAsync(documentId);

        Assert.NotNull(ids);
        Assert.Empty(ids);
    }

    [Fact]
    public async Task GetDocumentMissionIdsAsync_ReturnsDistinctOrdered_AndOnlyForDocument()
    {
        await using var testDb = new SqliteTestDb();

        var docA = Guid.NewGuid();
        var docB = Guid.NewGuid();

        // Make ordering deterministic
        var m1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var m2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var m3 = Guid.Parse("00000000-0000-0000-0000-000000000003");

        var nowUtc = new DateTime(2026, 2, 27, 12, 0, 0, DateTimeKind.Utc);

        await using (var ctx = testDb.Factory.CreateDbContext())
        {
            // Documents
            ctx.CombatTaskDocuments.AddRange(
                new CombatTaskDocument
                {
                    Id = docA,
                    Status = DocumentStatus.Active,
                    OrderTitle = "Doc A",
                    RecordedAt = new DateOnly(2026, 2, 27),
                    CreatedBy = "tests",
                    CreatedAtUtc = nowUtc
                },
                new CombatTaskDocument
                {
                    Id = docB,
                    Status = DocumentStatus.Active,
                    OrderTitle = "Doc B",
                    RecordedAt = new DateOnly(2026, 2, 27),
                    CreatedBy = "tests",
                    CreatedAtUtc = nowUtc
                });

            // Missions (FK required)
            ctx.Missions.AddRange(
                new Mission { Id = m1, PositionArea = "A", Target = "T", MissionMode = MissionMode.Day, CreatedAt = new DateOnly(2026, 2, 1) },
                new Mission { Id = m2, PositionArea = "A", Target = "T", MissionMode = MissionMode.Day, CreatedAt = new DateOnly(2026, 2, 1) },
                new Mission { Id = m3, PositionArea = "A", Target = "T", MissionMode = MissionMode.Day, CreatedAt = new DateOnly(2026, 2, 1) }
            );

            // Combat tasks: docA has m2 + m1 + duplicate m2; docB has m3
            ctx.CombatTasks.AddRange(
                new CombatTask { Id = Guid.NewGuid(), CombatTaskDocumentId = docA, MissionId = m2, SourceDocument = "S" },
                new CombatTask { Id = Guid.NewGuid(), CombatTaskDocumentId = docA, MissionId = m1, SourceDocument = "S" },
                new CombatTask { Id = Guid.NewGuid(), CombatTaskDocumentId = docA, MissionId = m2, SourceDocument = "S" },
                new CombatTask { Id = Guid.NewGuid(), CombatTaskDocumentId = docB, MissionId = m3, SourceDocument = "S" }
            );

            await ctx.SaveChangesAsync();
        }

        var repo = new CombatTaskRepository(testDb.Factory);
        var ids = await repo.GetDocumentMissionIdsAsync(docA);

        Assert.Equal(new[] { m1, m2 }, ids);
        Assert.DoesNotContain(m3, ids);
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static CombatTaskDetails NewDetails(
        Guid id,
        Guid personId,
        CombatTaskDetailsKind kind,
        DateOnly effectiveAt,
        string rnokpp,
        string fullName)
        => new()
        {
            Id = id,
            Kind = kind,
            EffectiveAt = effectiveAt,
            PersonId = personId,
            Rnokpp = rnokpp,
            FullName = fullName,
            Rank = "Рядовий",
            Position = "Оператор",
            Weapon = "AK",
            Callsign = "FOX"
        };

    private static async Task SeedActiveDocumentAsync(SqliteTestDb testDb, Guid documentId)
        => await SeedDocumentAsync(testDb, documentId, DocumentStatus.Active);

    private static async Task SeedDocumentAsync(SqliteTestDb testDb, Guid documentId, DocumentStatus status)
    {
        await using var db = testDb.Factory.CreateDbContext();

        db.Add(new CombatTaskDocument
        {
            Id = documentId,
            Status = status,
            OrderTitle = "Order",
            Description = "Desc",
            RecordedAt = new DateOnly(2026, 02, 01),
            CreatedBy = "tests",
            CreatedAtUtc = new DateTime(2026, 02, 01, 10, 0, 0, DateTimeKind.Utc),
            UpdatedBy = "tests",
            UpdatedAtUtc = new DateTime(2026, 02, 01, 10, 0, 0, DateTimeKind.Utc),
            CanceledBy = status == DocumentStatus.Canceled ? "tests" : null,
            CanceledAtUtc = status == DocumentStatus.Canceled ? new DateTime(2026, 02, 01, 11, 0, 0, DateTimeKind.Utc) : null,
            CanceledReason = status == DocumentStatus.Canceled ? "reason" : null
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedMissionAsync(SqliteTestDb testDb, Guid missionId)
    {
        await using var db = testDb.Factory.CreateDbContext();

        db.Add(new Mission
        {
            Id = missionId,
            PositionArea = "Area",
            NamePoint = "P1",
            TypeDrone = null,
            Target = "Target",
            MissionMode = MissionMode.FullTime,
            CreatedAt = new DateOnly(2026, 01, 01),
            ClosedAt = null
        });

        await db.SaveChangesAsync();
    }
}
