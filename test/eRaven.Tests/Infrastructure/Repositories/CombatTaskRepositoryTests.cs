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
    private static readonly DateTime NowUtc = new(2026, 02, 15, 12, 0, 0, DateTimeKind.Utc);

    //======================================================================
    // Seed helpers
    //======================================================================

    private static CombatTaskDocument NewDoc(
        string title,
        DateOnly recordedAt,
        DocumentStatus status = DocumentStatus.Draft)
        => new()
        {
            Id = Guid.NewGuid(),
            Status = status,
            OrderTitle = title,
            RecordedAt = recordedAt,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc.AddHours(-1)
        };

    private static Mission NewMission(
        string area,
        string namePoint,
        string target,
        MissionMode mode,
        DateOnly createdAt)
        => new()
        {
            Id = Guid.NewGuid(),
            PositionArea = area,
            NamePoint = namePoint,
            Target = target,
            MissionMode = mode,
            CreatedAt = createdAt,
            ClosedAt = null
        };

    private static CombatTaskDetails NewDetails(
        CombatTaskDetailsKind kind,
        DateOnly effectiveAt,
        Guid personId,
        string rnokpp = "1234567890",
        string fullName = "Test Person",
        string? callsign = "FOX",
        Guid? detailsId = null)
        => new()
        {
            Id = detailsId ?? Guid.Empty, // Guid.Empty дозволяємо — repo має згенерити
            CombatTaskId = Guid.Empty,     // repo проставить FK
            CombatTask = null,
            Kind = kind,
            EffectiveAt = effectiveAt,
            PersonId = personId,
            Rnokpp = rnokpp,
            FullName = fullName,
            Callsign = callsign
        };

    //======================================================================
    // Read: GetDocumentEditorAsync
    //======================================================================

    [Fact]
    public async Task GetDocumentEditorAsync_throws_when_document_id_is_empty()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetDocumentEditorAsync(Guid.Empty));
    }

    [Fact]
    public async Task GetDocumentEditorAsync_throws_when_document_not_found()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.GetDocumentEditorAsync(Guid.NewGuid()));

        Assert.Equal("Документ не знайдено.", ex.Message);
    }

    [Fact]
    public async Task GetDocumentEditorAsync_returns_document_header_and_mission_blocks_with_sorted_details()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("OPORD-EDITOR-001", new DateOnly(2026, 2, 1));
        var m1 = NewMission("AreaA", "P1", "T1", MissionMode.Day, new DateOnly(2026, 2, 1));
        var m2 = NewMission("AreaB", "P2", "T2", MissionMode.Night, new DateOnly(2026, 2, 1));

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        // Seed напряму => Id деталей НЕ Guid.Empty.
        var t1 = new CombatTask
        {
            Id = Guid.NewGuid(),
            SourceDocument = "SourceDoc1",
            CombatTaskDocumentId = doc.Id,
            MissionId = m1.Id,
            CombatTaskDetails =
            [
                NewDetails(CombatTaskDetailsKind.End,   new DateOnly(2026, 2, 10), p2, fullName: "B", detailsId: Guid.NewGuid()),
                NewDetails(CombatTaskDetailsKind.Start, new DateOnly(2026, 2, 05), p1, fullName: "A", detailsId: Guid.NewGuid()),
                NewDetails(CombatTaskDetailsKind.Start, new DateOnly(2026, 2, 05), p2, fullName: "B", detailsId: Guid.NewGuid())
            ]
        };

        var t2 = new CombatTask
        {
            Id = Guid.NewGuid(),
            SourceDocument = "SourceDoc2",
            CombatTaskDocumentId = doc.Id,
            MissionId = m2.Id,
            CombatTaskDetails =
            [
                NewDetails(CombatTaskDetailsKind.Start, new DateOnly(2026, 2, 03), p1, fullName: "A", detailsId: Guid.NewGuid()),
            ]
        };

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            db.Missions.AddRange(m1, m2);
            db.CombatTasks.AddRange(t1, t2);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskRepository(tdb.Factory);

        var editor = await repo.GetDocumentEditorAsync(doc.Id);

        // Header
        Assert.Equal(doc.Id, editor.DocumentId);
        Assert.Equal(doc.OrderTitle, editor.DocumentName);
        Assert.Equal(doc.Status, editor.Status);
        Assert.Equal(doc.RecordedAt, editor.RecordedAt);

        // Missions
        Assert.Equal(2, editor.Missions.Count);
        Assert.Contains(editor.Missions, x => x.MissionId == m1.Id);
        Assert.Contains(editor.Missions, x => x.MissionId == m2.Id);

        // SourceDocument mapped
        Assert.Contains(editor.Missions, x => x.SourceDocument == "SourceDoc1");
        Assert.Contains(editor.Missions, x => x.SourceDocument == "SourceDoc2");

        // Sorting in m1: EffectiveAt asc, Kind asc, FullName asc, Id
        var block1 = editor.Missions.Single(x => x.MissionId == m1.Id);

        Assert.Equal(t1.Id, block1.CombatTaskId);
        Assert.False(string.IsNullOrWhiteSpace(block1.MissionName));

        Assert.Equal(3, block1.CombatTaskDetails.Count);

        Assert.Equal(new DateOnly(2026, 2, 5), block1.CombatTaskDetails[0].EffectiveAt);
        Assert.Equal(CombatTaskDetailsKind.Start, block1.CombatTaskDetails[0].Kind);
        Assert.Equal("A", block1.CombatTaskDetails[0].FullName);

        Assert.Equal(new DateOnly(2026, 2, 5), block1.CombatTaskDetails[1].EffectiveAt);
        Assert.Equal(CombatTaskDetailsKind.Start, block1.CombatTaskDetails[1].Kind);
        Assert.Equal("B", block1.CombatTaskDetails[1].FullName);

        Assert.Equal(new DateOnly(2026, 2, 10), block1.CombatTaskDetails[2].EffectiveAt);
        Assert.Equal(CombatTaskDetailsKind.End, block1.CombatTaskDetails[2].Kind);

        Assert.All(block1.CombatTaskDetails, d => Assert.NotEqual(Guid.Empty, d.CombatTaskDetailsId));
    }

    //======================================================================
    // Write: CreateCombatTaskAsync
    //======================================================================

    [Fact]
    public async Task CreateCombatTaskAsync_throws_when_document_not_found()
    {
        await using var tdb = new SqliteTestDb();

        var mission = NewMission("AreaA", "P1", "T1", MissionMode.Day, new DateOnly(2026, 2, 1));
        using (var db = tdb.Factory.CreateDbContext())
        {
            db.Missions.Add(mission);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CreateCombatTaskAsync(
                documentId: Guid.NewGuid(),
                missionId: mission.Id,
                sourceDocument: "SD",
                combatTaskDetails: [],
                ct: default));

        Assert.Equal("Документ не знайдено.", ex.Message);
    }

    [Fact]
    public async Task CreateCombatTaskAsync_throws_when_document_is_posted()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("OPORD-CREATE-POSTED", new DateOnly(2026, 2, 1), status: DocumentStatus.Posted);
        var mission = NewMission("AreaA", "P1", "T1", MissionMode.Day, new DateOnly(2026, 2, 1));

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            db.Missions.Add(mission);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CreateCombatTaskAsync(doc.Id, mission.Id, "SD", []));

        Assert.Equal("Документ проведений і не може редагуватися.", ex.Message);
    }

    [Fact]
    public async Task CreateCombatTaskAsync_throws_when_document_is_canceled()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("OPORD-CREATE-CANCELED", new DateOnly(2026, 2, 1), status: DocumentStatus.Canceled);
        var mission = NewMission("AreaA", "P1", "T1", MissionMode.Day, new DateOnly(2026, 2, 1));

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            db.Missions.Add(mission);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CreateCombatTaskAsync(doc.Id, mission.Id, "SD", []));

        Assert.Equal("Документ відмінений і не може редагуватися.", ex.Message);
    }

    [Fact]
    public async Task CreateCombatTaskAsync_throws_when_block_for_mission_already_exists()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("OPORD-CREATE-DUP", new DateOnly(2026, 2, 1));
        var mission = NewMission("AreaA", "P1", "T1", MissionMode.Day, new DateOnly(2026, 2, 1));

        var existing = new CombatTask
        {
            Id = Guid.NewGuid(),
            CombatTaskDocumentId = doc.Id,
            MissionId = mission.Id,
            SourceDocument = "EXIST"
        };

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            db.Missions.Add(mission);
            db.CombatTasks.Add(existing);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CreateCombatTaskAsync(doc.Id, mission.Id, "SD", []));

        Assert.Equal("Блок по цій місії вже існує. Використайте UpsertCombatTaskAsync().", ex.Message);
    }

    [Fact]
    public async Task CreateCombatTaskAsync_sets_details_ids_fk_and_trims_source_document_and_persists()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("OPORD-CR-001", new DateOnly(2026, 2, 1));
        var mission = NewMission("AreaA", "P1", "T1", MissionMode.Day, new DateOnly(2026, 2, 1));

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            db.Missions.Add(mission);
            await db.SaveChangesAsync();
        }

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        // Guid.Empty => repo має згенерити Id.
        var details = new List<CombatTaskDetails>
        {
            NewDetails(CombatTaskDetailsKind.Start, new DateOnly(2026, 2, 2), p1, fullName: "A"),
            NewDetails(CombatTaskDetailsKind.End,   new DateOnly(2026, 2, 7), p2, fullName: "B"),
        };

        var repo = new CombatTaskRepository(tdb.Factory);

        var taskId = await repo.CreateCombatTaskAsync(
            documentId: doc.Id,
            missionId: mission.Id,
            sourceDocument: "  SD-001  ",
            combatTaskDetails: details);

        Assert.NotEqual(Guid.Empty, taskId);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();

        var task = await db2.CombatTasks
            .AsNoTracking()
            .Include(x => x.CombatTaskDetails)
            .FirstOrDefaultAsync(x => x.Id == taskId);

        Assert.NotNull(task);
        Assert.Equal(doc.Id, task!.CombatTaskDocumentId);
        Assert.Equal(mission.Id, task.MissionId);
        Assert.Equal("SD-001", task.SourceDocument);

        Assert.Equal(2, task.CombatTaskDetails.Count);

        foreach (var d in task.CombatTaskDetails)
        {
            Assert.NotEqual(Guid.Empty, d.Id);
            Assert.Equal(taskId, d.CombatTaskId);
        }
    }

    [Fact]
    public async Task CreateCombatTaskAsync_validates_duplicate_person_kind_date_and_throws()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("OPORD-CR-002", new DateOnly(2026, 2, 1));
        var mission = NewMission("AreaA", "P1", "T1", MissionMode.FullTime, new DateOnly(2026, 2, 1));

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            db.Missions.Add(mission);
            await db.SaveChangesAsync();
        }

        var p1 = Guid.NewGuid();
        var date = new DateOnly(2026, 2, 2);

        var details = new List<CombatTaskDetails>
        {
            NewDetails(CombatTaskDetailsKind.Start, date, p1, fullName: "A"),
            NewDetails(CombatTaskDetailsKind.Start, date, p1, fullName: "A2"), // дублікат
        };

        var repo = new CombatTaskRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CreateCombatTaskAsync(doc.Id, mission.Id, "SD-002", details));

        Assert.StartsWith("Дублікат рядка:", ex.Message);
    }

    [Fact]
    public async Task CreateCombatTaskAsync_validates_required_fields_and_throws_on_empty_personId()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("OPORD-CR-REQ-1", new DateOnly(2026, 2, 1));
        var mission = NewMission("AreaA", "P1", "T1", MissionMode.Day, new DateOnly(2026, 2, 1));

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            db.Missions.Add(mission);
            await db.SaveChangesAsync();
        }

        var details = new List<CombatTaskDetails>
        {
            NewDetails(CombatTaskDetailsKind.Start, new DateOnly(2026, 2, 2), Guid.Empty, fullName: "A"),
        };

        var repo = new CombatTaskRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CreateCombatTaskAsync(doc.Id, mission.Id, "SD", details));

        Assert.Equal("Рядок містить порожній PersonId.", ex.Message);
    }

    //======================================================================
    // Write: UpsertCombatTaskAsync
    //======================================================================

    [Fact]
    public async Task UpsertCombatTaskAsync_when_task_missing_creates_new_and_returns_new_id()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("OPORD-UP-NEW", new DateOnly(2026, 2, 1));
        var mission = NewMission("AreaA", "P1", "T1", MissionMode.Day, new DateOnly(2026, 2, 1));
        var p1 = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            db.Missions.Add(mission);
            await db.SaveChangesAsync();
        }

        var details = new List<CombatTaskDetails>
        {
            NewDetails(CombatTaskDetailsKind.Start, new DateOnly(2026, 2, 2), p1, fullName: "A"),
        };

        var repo = new CombatTaskRepository(tdb.Factory);

        var taskId = await repo.UpsertCombatTaskAsync(doc.Id, mission.Id, "  SRC  ", details);

        Assert.NotEqual(Guid.Empty, taskId);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var task = await db2.CombatTasks.AsNoTracking()
            .Include(x => x.CombatTaskDetails)
            .SingleAsync(x => x.Id == taskId);

        Assert.Equal("SRC", task.SourceDocument);
        Assert.Single(task.CombatTaskDetails);
    }

    [Fact]
    public async Task UpsertCombatTaskAsync_when_task_exists_replaces_details_and_updates_source_document()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("OPORD-UP-REPLACE", new DateOnly(2026, 2, 1));
        var mission = NewMission("AreaA", "P1", "T1", MissionMode.Day, new DateOnly(2026, 2, 1));

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var task = new CombatTask
        {
            Id = Guid.NewGuid(),
            CombatTaskDocumentId = doc.Id,
            MissionId = mission.Id,
            SourceDocument = "OLD",
            CombatTaskDetails =
            [
                NewDetails(CombatTaskDetailsKind.Start, new DateOnly(2026, 2, 2), p1, fullName: "A", detailsId: Guid.NewGuid()),
                NewDetails(CombatTaskDetailsKind.End,   new DateOnly(2026, 2, 3), p1, fullName: "A", detailsId: Guid.NewGuid())
            ]
        };

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            db.Missions.Add(mission);
            db.CombatTasks.Add(task);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskRepository(tdb.Factory);

        // Новий набір рядків: замінює повністю.
        var newLines = new List<CombatTaskDetails>
        {
            NewDetails(CombatTaskDetailsKind.Start, new DateOnly(2026, 2, 10), p2, fullName: "B"),
        };

        var returnedId = await repo.UpsertCombatTaskAsync(doc.Id, mission.Id, " NEW ", newLines);

        Assert.Equal(task.Id, returnedId);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();

        var reloaded = await db2.CombatTasks.AsNoTracking()
            .Include(x => x.CombatTaskDetails)
            .SingleAsync(x => x.Id == task.Id);

        Assert.Equal("NEW", reloaded.SourceDocument);
        Assert.Single(reloaded.CombatTaskDetails);

        var only = reloaded.CombatTaskDetails.Single();
        Assert.Equal(p2, only.PersonId);
        Assert.Equal(new DateOnly(2026, 2, 10), only.EffectiveAt);
        Assert.Equal(CombatTaskDetailsKind.Start, only.Kind);
    }

    [Fact]
    public async Task UpsertCombatTaskAsync_throws_when_document_is_not_draft()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("OPORD-UP-NODRAFT", new DateOnly(2026, 2, 1), status: DocumentStatus.Posted);
        var mission = NewMission("AreaA", "P1", "T1", MissionMode.Day, new DateOnly(2026, 2, 1));

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            db.Missions.Add(mission);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.UpsertCombatTaskAsync(doc.Id, mission.Id, "SD", []));

        Assert.Equal("Документ проведений і не може редагуватися.", ex.Message);
    }

    //======================================================================
    // Write: DeleteCombatTaskAsync
    //======================================================================

    [Fact]
    public async Task DeleteCombatTaskAsync_when_document_not_found_should_noop()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskRepository(tdb.Factory);

        await repo.DeleteCombatTaskAsync(Guid.NewGuid(), Guid.NewGuid());

        // no throw => ok
    }

    [Fact]
    public async Task DeleteCombatTaskAsync_when_document_is_posted_should_throw()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("OPORD-DEL-POSTED", new DateOnly(2026, 2, 1), status: DocumentStatus.Posted);
        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.DeleteCombatTaskAsync(doc.Id, Guid.NewGuid()));

        Assert.Equal("Документ проведений і не може редагуватися.", ex.Message);
    }

    [Fact]
    public async Task DeleteCombatTaskAsync_when_task_not_found_should_noop()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("OPORD-DEL-NOTFOUND", new DateOnly(2026, 2, 1));
        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskRepository(tdb.Factory);

        await repo.DeleteCombatTaskAsync(doc.Id, Guid.NewGuid());

        // no throw => ok
    }

    [Fact]
    public async Task DeleteCombatTaskAsync_deletes_task_and_cascades_details()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("OPORD-DEL-001", new DateOnly(2026, 2, 1));
        var mission = NewMission("AreaA", "P1", "T1", MissionMode.Day, new DateOnly(2026, 2, 1));
        var p1 = Guid.NewGuid();

        var task = new CombatTask
        {
            Id = Guid.NewGuid(),
            SourceDocument = "SD-DEL",
            CombatTaskDocumentId = doc.Id,
            MissionId = mission.Id,
            CombatTaskDetails =
            [
                NewDetails(CombatTaskDetailsKind.Start, new DateOnly(2026, 2, 2), p1, fullName: "A", detailsId: Guid.NewGuid()),
            ]
        };

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            db.Missions.Add(mission);
            db.CombatTasks.Add(task);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskRepository(tdb.Factory);

        await repo.DeleteCombatTaskAsync(doc.Id, task.Id);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();

        var existsTask = await db2.CombatTasks.AsNoTracking().AnyAsync(x => x.Id == task.Id);
        Assert.False(existsTask);

        var existsDetails = await db2.CombatTaskDetails.AsNoTracking().AnyAsync(x => x.CombatTaskId == task.Id);
        Assert.False(existsDetails);
    }
}
