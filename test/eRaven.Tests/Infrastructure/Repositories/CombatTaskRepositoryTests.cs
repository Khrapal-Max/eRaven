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
    private static readonly DateTime NowUtc = new(2026, 02, 08, 8, 0, 0, DateTimeKind.Utc);

    //======================================================================
    // Seed helpers
    //======================================================================

    private static CombatTaskDocument NewDoc(string title, DateOnly recordedAt)
        => new()
        {
            Id = Guid.NewGuid(),
            Status = DocumentStatus.Draft,
            OrderTitle = title,
            RecordedAt = recordedAt,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc
        };

    private static Mission NewMission(string area, string namePoint, string target, MissionMode mode, DateOnly createdAt)
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
            Id = detailsId ?? Guid.Empty, // Guid.Empty дозволяємо лише коли repo має згенерити
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
    // Tests: Read
    //======================================================================

    [Fact]
    public async Task GetDocumentEditorAsync_throws_when_document_not_found()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskRepository(tdb.Factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.GetDocumentEditorAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetDocumentEditorAsync_returns_document_header_and_grouped_missions_with_sorted_details()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("OPORD-001", new DateOnly(2026, 2, 1));
        var m1 = NewMission("AreaA", "P1", "T1", MissionMode.Day, new DateOnly(2026, 2, 1));
        var m2 = NewMission("AreaB", "P2", "T2", MissionMode.Night, new DateOnly(2026, 2, 1));

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        // Seed: 2 combat tasks (по місіях), details у випадковому порядку.
        // ВАЖЛИВО: seed напряму => Id деталей має бути НЕ Guid.Empty.
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

        // Grouping
        Assert.Equal(2, editor.Missions.Count);
        Assert.Contains(editor.Missions, x => x.MissionId == m1.Id);
        Assert.Contains(editor.Missions, x => x.MissionId == m2.Id);

        // SourceDocument mapped
        Assert.Contains(editor.Missions, x => x.SourceDocument == "SourceDoc1");
        Assert.Contains(editor.Missions, x => x.SourceDocument == "SourceDoc2");

        // Sorting in m1 block: EffectiveAt asc, Kind asc, FullName asc, Id
        var block1 = editor.Missions.Single(x => x.MissionId == m1.Id);

        Assert.Equal(t1.Id, block1.CombatTaskId);
        Assert.Equal("SourceDoc1", block1.SourceDocument);

        Assert.Equal(3, block1.CombatTaskDetails.Count);

        Assert.Equal(new DateOnly(2026, 2, 5), block1.CombatTaskDetails[0].EffectiveAt);
        Assert.Equal(CombatTaskDetailsKind.Start, block1.CombatTaskDetails[0].Kind);
        Assert.Equal("A", block1.CombatTaskDetails[0].FullName);

        Assert.Equal(new DateOnly(2026, 2, 5), block1.CombatTaskDetails[1].EffectiveAt);
        Assert.Equal(CombatTaskDetailsKind.Start, block1.CombatTaskDetails[1].Kind);
        Assert.Equal("B", block1.CombatTaskDetails[1].FullName);

        Assert.Equal(new DateOnly(2026, 2, 10), block1.CombatTaskDetails[2].EffectiveAt);
        Assert.Equal(CombatTaskDetailsKind.End, block1.CombatTaskDetails[2].Kind);

        // DTO ids mapped
        Assert.All(block1.CombatTaskDetails, d => Assert.NotEqual(Guid.Empty, d.CombatTaskDetailsId));
    }

    //======================================================================
    // Tests: Create
    //======================================================================

    [Fact]
    public async Task CreateCombatTask_sets_details_ids_and_fk_and_persists()
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

        // Тут навмисно даємо Guid.Empty — repo має згенерити Id.
        var details = new List<CombatTaskDetails>
        {
            NewDetails(CombatTaskDetailsKind.Start, new DateOnly(2026, 2, 2), p1, fullName: "A"),
            NewDetails(CombatTaskDetailsKind.End,   new DateOnly(2026, 2, 7), p2, fullName: "B"),
        };

        var repo = new CombatTaskRepository(tdb.Factory);

        var taskId = await repo.CreateCombatTask(
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
        Assert.Equal("SD-001", task.SourceDocument); // trim перевіряємо
        Assert.Equal(2, task.CombatTaskDetails.Count);

        foreach (var d in task.CombatTaskDetails)
        {
            Assert.NotEqual(Guid.Empty, d.Id);
            Assert.Equal(taskId, d.CombatTaskId);
        }
    }

    [Fact]
    public async Task CreateCombatTask_throws_on_duplicate_key_person_kind_date()
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

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CreateCombatTask(
                documentId: doc.Id,
                missionId: mission.Id,
                sourceDocument: "SD-002",
                combatTaskDetails: details));
    }

    //======================================================================
    // Tests: Update
    //======================================================================

    [Fact]
    public async Task UpdateCombatTask_replaces_details_and_updates_mission()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("OPORD-UP-001", new DateOnly(2026, 2, 1));
        var m1 = NewMission("AreaA", "P1", "T1", MissionMode.Day, new DateOnly(2026, 2, 1));
        var m2 = NewMission("AreaB", "P2", "T2", MissionMode.Night, new DateOnly(2026, 2, 1));

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        // Seed existing task with ONE existing details row.
        // ВАЖЛИВО: seed напряму => detailsId must NOT be Guid.Empty.
        var task = new CombatTask
        {
            Id = Guid.NewGuid(),
            SourceDocument = "OLD-SD",
            CombatTaskDocumentId = doc.Id,
            MissionId = m1.Id,
            CombatTaskDetails =
            [
                NewDetails(CombatTaskDetailsKind.Start, new DateOnly(2026, 2, 2), p1, fullName: "OLD", detailsId: Guid.NewGuid())
            ]
        };

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            db.Missions.AddRange(m1, m2);
            db.CombatTasks.Add(task);
            await db.SaveChangesAsync();
        }

        // New set of details (повна заміна) — Guid.Empty, repo згенерує.
        var newDetails = new List<CombatTaskDetails>
        {
            NewDetails(CombatTaskDetailsKind.Start, new DateOnly(2026, 2, 3), p1, fullName: "A"),
            NewDetails(CombatTaskDetailsKind.End,   new DateOnly(2026, 2, 7), p2, fullName: "B")
        };

        var repo = new CombatTaskRepository(tdb.Factory);

        var updatedId = await repo.UpdateCombatTask(doc.Id, task.Id, m2.Id, newDetails);

        Assert.Equal(task.Id, updatedId);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();

        var stored = await db2.CombatTasks
            .AsNoTracking()
            .Include(x => x.CombatTaskDetails)
            .FirstAsync(x => x.Id == task.Id);

        Assert.Equal(m2.Id, stored.MissionId);
        Assert.Equal("OLD-SD", stored.SourceDocument); // Update зараз не змінює sourceDocument
        Assert.Equal(2, stored.CombatTaskDetails.Count);

        Assert.DoesNotContain(stored.CombatTaskDetails, x => x.FullName == "OLD");

        foreach (var d in stored.CombatTaskDetails)
        {
            Assert.NotEqual(Guid.Empty, d.Id);
            Assert.Equal(task.Id, d.CombatTaskId);
        }
    }

    //======================================================================
    // Tests: Delete
    //======================================================================

    [Fact]
    public async Task DeleteCombatTask_deletes_task_and_cascades_details()
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

        await repo.DeleteCombatTask(doc.Id, task.Id);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();

        var existsTask = await db2.CombatTasks.AsNoTracking().AnyAsync(x => x.Id == task.Id);
        Assert.False(existsTask);

        var existsDetails = await db2.CombatTaskDetails.AsNoTracking().AnyAsync(x => x.CombatTaskId == task.Id);
        Assert.False(existsDetails);
    }
}
