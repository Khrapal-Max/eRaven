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

/// <summary>
/// Тести для <see cref="CombatTaskRepository"/>.
///
/// <para>
/// Фіксуємо контракт "документ → контент (CombatTask + CombatTaskDetails)":
/// <list type="bullet">
/// <item><description>читання editor DTO з сортуванням рядків;</description></item>
/// <item><description>Create/Upsert з trim і коректним збереженням snapshot-рядків;</description></item>
/// <item><description>Upsert замінює всі details (replace-all);</description></item>
/// <item><description>Delete видаляє CombatTask та каскадно details;</description></item>
/// <item><description>заборона редагування для Canceled документа;</description></item>
/// <item><description>GetDocumentMissionIdsAsync повертає MissionId для документа.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class CombatTaskRepositoryTests
{
    //======================================================================
    // Read: Editor
    //======================================================================

    /// <summary>
    /// GetDocumentEditorAsync повертає header документа та місії з деталями,
    /// а деталі відсортовані: EffectiveAt → Kind → FullName → Id.
    /// </summary>
    [Fact]
    public async Task GetDocumentEditorAsync_ReturnsHeaderAndSortedMissionBlocks()
    {
        await using var testDb = new SqliteTestDb();

        var docRepo = new CombatTaskDocumentRepository(testDb.Factory);
        var repo = new CombatTaskRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        // Seed: document
        var docId = await docRepo.CreateAsync(
            orderTitle: "Наказ №1",
            recordedAt: new DateOnly(2026, 02, 10),
            description: "D",
            author: "seed",
            nowUtc: now);

        // Seed: mission (required FK)
        var missionId = await SeedMissionAsync(testDb, id: Guid.Parse("00000000-0000-0000-0000-000000000010"));

        // Create combat task with details unsorted
        var d1 = NewDetail(CombatTaskDetailsKind.End, new DateOnly(2026, 02, 12), "B");
        var d2 = NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), "A");
        var d3 = NewDetail(CombatTaskDetailsKind.End, new DateOnly(2026, 02, 10), "C");

        var taskId = await repo.CreateCombatTaskAsync(
            documentId: docId,
            missionId: missionId,
            sourceDocument: " SRC ",
            combatTaskDetails: [d1, d2, d3]);

        var editor = await repo.GetDocumentEditorAsync(docId);

        Assert.Equal(docId, editor.DocumentId);
        Assert.Equal("Наказ №1", editor.DocumentName);
        Assert.Equal("D", editor.Description);
        Assert.Equal(DocumentStatus.Active, editor.Status);
        Assert.Equal(new DateOnly(2026, 02, 10), editor.RecordedAt);

        Assert.Single(editor.Missions);

        var block = editor.Missions[0];
        Assert.Equal(taskId, block.CombatTaskId);
        Assert.Equal(missionId, block.MissionId);
        Assert.Equal("SRC", block.SourceDocument);

        // MissionName uses Mission.ToString()
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var mission = await db.Missions.SingleAsync(x => x.Id == missionId);
            Assert.Equal(mission.ToString(), block.MissionName);
        }

        // Sorted by EffectiveAt, Kind (Start=1 before End=2), FullName
        Assert.Equal(3, block.CombatTaskDetails.Count);
        Assert.Equal("A", block.CombatTaskDetails[0].FullName);
        Assert.Equal(CombatTaskDetailsKind.Start, block.CombatTaskDetails[0].Kind);
        Assert.Equal(new DateOnly(2026, 02, 10), block.CombatTaskDetails[0].EffectiveAt);

        Assert.Equal("C", block.CombatTaskDetails[1].FullName);
        Assert.Equal(CombatTaskDetailsKind.End, block.CombatTaskDetails[1].Kind);
        Assert.Equal(new DateOnly(2026, 02, 10), block.CombatTaskDetails[1].EffectiveAt);

        Assert.Equal("B", block.CombatTaskDetails[2].FullName);
        Assert.Equal(CombatTaskDetailsKind.End, block.CombatTaskDetails[2].Kind);
        Assert.Equal(new DateOnly(2026, 02, 12), block.CombatTaskDetails[2].EffectiveAt);
    }

    //======================================================================
    // Write: Create
    //======================================================================

    /// <summary>
    /// CreateCombatTaskAsync зберігає CombatTask і всі snapshot-рядки, trim'ить SourceDocument,
    /// та виставляє FK CombatTaskId для деталей через relationship fix-up.
    /// </summary>
    [Fact]
    public async Task CreateCombatTaskAsync_PersistsTaskAndDetails_AndTrimsSource()
    {
        await using var testDb = new SqliteTestDb();

        var docRepo = new CombatTaskDocumentRepository(testDb.Factory);
        var repo = new CombatTaskRepository(testDb.Factory);

        var docId = await docRepo.CreateAsync(
            orderTitle: "Наказ №2",
            recordedAt: new DateOnly(2026, 02, 10),
            description: null,
            author: "seed",
            nowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc));

        var missionId = await SeedMissionAsync(testDb, id: Guid.Parse("00000000-0000-0000-0000-000000000020"));

        var details = new[]
        {
            NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), "P1"),
            NewDetail(CombatTaskDetailsKind.End,   new DateOnly(2026, 02, 11), "P1")
        };

        var taskId = await repo.CreateCombatTaskAsync(
            documentId: docId,
            missionId: missionId,
            sourceDocument: "  SRC-2  ",
            combatTaskDetails: details);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        var task = await db.CombatTasks
            .Include(x => x.CombatTaskDetails)
            .SingleAsync(x => x.Id == taskId);

        Assert.Equal(docId, task.CombatTaskDocumentId);
        Assert.Equal(missionId, task.MissionId);
        Assert.Equal("SRC-2", task.SourceDocument);

        Assert.Equal(2, task.CombatTaskDetails.Count);
        Assert.All(task.CombatTaskDetails, d => Assert.Equal(taskId, d.CombatTaskId));
        Assert.All(task.CombatTaskDetails, d => Assert.False(string.IsNullOrWhiteSpace(d.Rnokpp)));
        Assert.All(task.CombatTaskDetails, d => Assert.False(string.IsNullOrWhiteSpace(d.FullName)));
    }

    /// <summary>
    /// CreateCombatTaskAsync кидає помилку, якщо для MissionId у документі вже існує завдання.
    /// </summary>
    [Fact]
    public async Task CreateCombatTaskAsync_Throws_WhenTaskAlreadyExistsForMission()
    {
        await using var testDb = new SqliteTestDb();

        var docRepo = new CombatTaskDocumentRepository(testDb.Factory);
        var repo = new CombatTaskRepository(testDb.Factory);

        var docId = await docRepo.CreateAsync(
            orderTitle: "Наказ №3",
            recordedAt: new DateOnly(2026, 02, 10),
            description: null,
            author: "seed",
            nowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc));

        var missionId = await SeedMissionAsync(testDb, id: Guid.Parse("00000000-0000-0000-0000-000000000030"));

        await repo.CreateCombatTaskAsync(
            documentId: docId,
            missionId: missionId,
            sourceDocument: "SRC",
            combatTaskDetails: [NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), "P")]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CreateCombatTaskAsync(
                documentId: docId,
                missionId: missionId,
                sourceDocument: "SRC2",
                combatTaskDetails: [NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), "P2")]));

        Assert.Contains("вже існує", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Create/Upsert/Delete мають бути заборонені для Canceled документа.
    /// </summary>
    [Fact]
    public async Task WriteOperations_Throw_WhenDocumentCanceled()
    {
        await using var testDb = new SqliteTestDb();

        var docRepo = new CombatTaskDocumentRepository(testDb.Factory);
        var repo = new CombatTaskRepository(testDb.Factory);

        var createdAt = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var docId = await docRepo.CreateAsync(
            orderTitle: "Наказ №4",
            recordedAt: new DateOnly(2026, 02, 10),
            description: null,
            author: "seed",
            nowUtc: createdAt);

        await docRepo.CancelAsync(
            documentId: docId,
            reason: "X",
            author: "auditor",
            nowUtc: createdAt.AddMinutes(1));

        var missionId = await SeedMissionAsync(testDb, id: Guid.Parse("00000000-0000-0000-0000-000000000040"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CreateCombatTaskAsync(
                documentId: docId,
                missionId: missionId,
                sourceDocument: "SRC",
                combatTaskDetails: [NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), "P")]));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.UpsertCombatTaskAsync(
                documentId: docId,
                missionId: missionId,
                sourceDocument: "SRC",
                combatTaskDetails: [NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), "P")]));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.DeleteCombatTaskAsync(
                documentId: docId,
                combatTaskId: Guid.NewGuid()));
    }

    //======================================================================
    // Write: Upsert
    //======================================================================

    /// <summary>
    /// UpsertCombatTaskAsync:
    /// <list type="bullet">
    /// <item><description>створює новий CombatTask, якщо його ще немає;</description></item>
    /// <item><description>якщо є — оновлює SourceDocument та повністю замінює details (replace-all).</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task UpsertCombatTaskAsync_CreatesThenReplacesDetails()
    {
        await using var testDb = new SqliteTestDb();

        var docRepo = new CombatTaskDocumentRepository(testDb.Factory);
        var repo = new CombatTaskRepository(testDb.Factory);

        var docId = await docRepo.CreateAsync(
            orderTitle: "Наказ №5",
            recordedAt: new DateOnly(2026, 02, 10),
            description: null,
            author: "seed",
            nowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc));

        var missionId = await SeedMissionAsync(testDb, id: Guid.Parse("00000000-0000-0000-0000-000000000050"));

        var oldDetailId = Guid.NewGuid();

        var id1 = await repo.UpsertCombatTaskAsync(
            documentId: docId,
            missionId: missionId,
            sourceDocument: "  SRC-OLD  ",
            combatTaskDetails:
            [
                NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), "Old", detailId: oldDetailId)
            ]);

        // Upsert again with a different set (replace-all)
        var newDetail1 = Guid.NewGuid();
        var newDetail2 = Guid.NewGuid();

        var id2 = await repo.UpsertCombatTaskAsync(
            documentId: docId,
            missionId: missionId,
            sourceDocument: "  SRC-NEW  ",
            combatTaskDetails:
            [
                NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), "NewA", detailId: newDetail1),
                NewDetail(CombatTaskDetailsKind.End,   new DateOnly(2026, 02, 11), "NewA", detailId: newDetail2)
            ]);

        Assert.Equal(id1, id2);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        var task = await db.CombatTasks
            .Include(x => x.CombatTaskDetails)
            .SingleAsync(x => x.Id == id1);

        Assert.Equal("SRC-NEW", task.SourceDocument);
        Assert.Equal(2, task.CombatTaskDetails.Count);

        // old detail removed
        Assert.DoesNotContain(task.CombatTaskDetails, d => d.Id == oldDetailId);

        // new details exist
        Assert.Contains(task.CombatTaskDetails, d => d.Id == newDetail1);
        Assert.Contains(task.CombatTaskDetails, d => d.Id == newDetail2);
    }

    //======================================================================
    // Write: Delete
    //======================================================================

    /// <summary>
    /// DeleteCombatTaskAsync видаляє CombatTask і каскадно всі CombatTaskDetails.
    /// </summary>
    [Fact]
    public async Task DeleteCombatTaskAsync_RemovesTaskAndDetails()
    {
        await using var testDb = new SqliteTestDb();

        var docRepo = new CombatTaskDocumentRepository(testDb.Factory);
        var repo = new CombatTaskRepository(testDb.Factory);

        var docId = await docRepo.CreateAsync(
            orderTitle: "Наказ №6",
            recordedAt: new DateOnly(2026, 02, 10),
            description: null,
            author: "seed",
            nowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc));

        var missionId = await SeedMissionAsync(testDb, id: Guid.Parse("00000000-0000-0000-0000-000000000060"));

        var taskId = await repo.CreateCombatTaskAsync(
            documentId: docId,
            missionId: missionId,
            sourceDocument: "SRC",
            combatTaskDetails:
            [
                NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), "P1"),
                NewDetail(CombatTaskDetailsKind.End,   new DateOnly(2026, 02, 11), "P1")
            ]);

        // Ensure saved
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            Assert.True(await db.CombatTasks.AnyAsync(x => x.Id == taskId));
            Assert.True(await db.CombatTaskDetails.AnyAsync(x => x.CombatTaskId == taskId));
        }

        await repo.DeleteCombatTaskAsync(documentId: docId, combatTaskId: taskId);

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            Assert.False(await db.CombatTasks.AnyAsync(x => x.Id == taskId));
            Assert.False(await db.CombatTaskDetails.AnyAsync(x => x.CombatTaskId == taskId));
        }
    }

    /// <summary>
    /// DeleteCombatTaskAsync — no-op, якщо завдання не знайдено.
    /// </summary>
    [Fact]
    public async Task DeleteCombatTaskAsync_NoOp_WhenTaskNotFound()
    {
        await using var testDb = new SqliteTestDb();

        var docRepo = new CombatTaskDocumentRepository(testDb.Factory);
        var repo = new CombatTaskRepository(testDb.Factory);

        var docId = await docRepo.CreateAsync(
            orderTitle: "Наказ №7",
            recordedAt: new DateOnly(2026, 02, 10),
            description: null,
            author: "seed",
            nowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc));

        // Should not throw
        await repo.DeleteCombatTaskAsync(documentId: docId, combatTaskId: Guid.NewGuid());
    }

    //======================================================================
    // Read: GetDocumentMissionIdsAsync (узгодженість контракту)
    //======================================================================

    /// <summary>
    /// GetDocumentMissionIdsAsync повертає MissionId[] для документа (відсортовано).
    /// </summary>
    [Fact]
    public async Task GetDocumentMissionIdsAsync_ReturnsOrderedMissionIds()
    {
        await using var testDb = new SqliteTestDb();

        var docRepo = new CombatTaskDocumentRepository(testDb.Factory);
        var repo = new CombatTaskRepository(testDb.Factory);

        var docId = await docRepo.CreateAsync(
            orderTitle: "Наказ №8",
            recordedAt: new DateOnly(2026, 02, 10),
            description: null,
            author: "seed",
            nowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc));

        var m1 = Guid.Parse("00000000-0000-0000-0000-000000000101");
        var m2 = Guid.Parse("00000000-0000-0000-0000-000000000102");

        await SeedMissionAsync(testDb, id: m2);
        await SeedMissionAsync(testDb, id: m1);

        await repo.CreateCombatTaskAsync(
            documentId: docId,
            missionId: m2,
            sourceDocument: "S2",
            combatTaskDetails: [NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), "P")]);

        await repo.CreateCombatTaskAsync(
            documentId: docId,
            missionId: m1,
            sourceDocument: "S1",
            combatTaskDetails: [NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), "P")]);

        var ids = await repo.GetDocumentMissionIdsAsync(docId);

        Assert.Equal(2, ids.Count);
        Assert.Equal(m1, ids[0]);
        Assert.Equal(m2, ids[1]);
    }

    //======================================================================
    // Helpers
    //======================================================================

    /// <summary>
    /// Створює мінімально валідний Mission (для FK у CombatTask).
    /// </summary>
    private static async Task<Guid> SeedMissionAsync(SqliteTestDb testDb, Guid? id = null)
    {
        var missionId = id ?? Guid.NewGuid();

        await using var db = await testDb.Factory.CreateDbContextAsync();

        if (await db.Missions.AnyAsync(x => x.Id == missionId))
            return missionId;

        db.Missions.Add(new Mission
        {
            Id = missionId,
            PositionArea = "Area-1",
            NamePoint = null,
            TypeDrone = "UAS",
            Target = "Target",
            MissionMode = MissionMode.Day,
            CreatedAt = new DateOnly(2026, 02, 01),
            ClosedAt = null
        });

        await db.SaveChangesAsync();
        return missionId;
    }

    /// <summary>
    /// Створює snapshot-рядок CombatTaskDetails з мінімально валідними полями.
    /// </summary>
    private static CombatTaskDetails NewDetail(
        CombatTaskDetailsKind kind,
        DateOnly effectiveAt,
        string fullName,
        Guid? detailId = null)
        => new()
        {
            Id = detailId ?? Guid.NewGuid(),
            Kind = kind,
            EffectiveAt = effectiveAt,
            PersonId = Guid.NewGuid(),
            Rnokpp = "1234567890",
            FullName = fullName,
            Rank = null,
            Position = null,
            Weapon = null,
            Callsign = null
        };
}
