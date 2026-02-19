//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskDetailsByDocumentIdQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Application.Handlers.CombatTasks;
using eRaven.Application.Queries.CombatTasks;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Moq;

namespace eRaven.Tests.Application.Handlers.CombatTasks;

public sealed class GetCombatTaskDetailsByDocumentIdQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_MapsHeader_AndSortsMissionsAndDetails()
    {
        // arrange
        var documentId = Guid.NewGuid();

        var repo = new Mock<ICombatTaskRepository>(MockBehavior.Strict);

        var doc = NewDocument(documentId);

        // Missions
        var mission1 = NewMission(Guid.Parse("00000000-0000-0000-0000-000000000001"), "A", "T1");
        var mission2 = NewMission(Guid.Parse("00000000-0000-0000-0000-000000000002"), "B", "T2");

        // Tasks are intentionally added unsorted: mission2 first, then mission1
        var task2 = NewTask(doc,
            combatTaskId: Guid.Parse("00000000-0000-0000-0000-000000000102"),
            missionId: mission2.Id,
            mission: mission2,
            sourceDocument: "SRC-2");

        var task1 = NewTask(doc,
            combatTaskId: Guid.Parse("00000000-0000-0000-0000-000000000101"),
            missionId: mission1.Id,
            mission: mission1,
            sourceDocument: "SRC-1");

        // Details for task1 are intentionally unsorted
        AddDetail(task1,
            id: Guid.Parse("00000000-0000-0000-0000-000000000201"),
            kind: CombatTaskDetailsKind.End,
            effectiveAt: new DateOnly(2026, 02, 12),
            fullName: "B");

        AddDetail(task1,
            id: Guid.Parse("00000000-0000-0000-0000-000000000202"),
            kind: CombatTaskDetailsKind.Start,
            effectiveAt: new DateOnly(2026, 02, 10),
            fullName: "A");

        AddDetail(task1,
            id: Guid.Parse("00000000-0000-0000-0000-000000000203"),
            kind: CombatTaskDetailsKind.End,
            effectiveAt: new DateOnly(2026, 02, 10),
            fullName: "C");

        // Details for task2 (single row)
        AddDetail(task2,
            id: Guid.Parse("00000000-0000-0000-0000-000000000204"),
            kind: CombatTaskDetailsKind.Start,
            effectiveAt: new DateOnly(2026, 02, 11),
            fullName: "X");

        doc.CombatTasks.Add(task2);
        doc.CombatTasks.Add(task1);

        repo.Setup(r => r.GetDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var handler = new GetCombatTaskDetailsByDocumentIdQueryHandler(repo.Object);

        // act
        var res = await handler.HandleAsync(new GetCombatTaskDetailsByDocumentIdQuery(documentId));

        // assert: header mapping
        Assert.NotNull(res);
        Assert.Equal(doc.Id, res!.DocumentId);
        Assert.Equal(doc.OrderTitle, res.DocumentName);
        Assert.Equal(doc.Description, res.Description);
        Assert.Equal(doc.Status, res.Status);
        Assert.Equal(doc.RecordedAt, res.RecordedAt);

        // assert: missions/tasks sorting (MissionId -> TaskId)
        var expectedTasks = doc.CombatTasks
            .OrderBy(x => x.MissionId)
            .ThenBy(x => x.Id)
            .ToList();

        Assert.Equal(expectedTasks.Count, res.Missions.Count);

        for (var i = 0; i < expectedTasks.Count; i++)
        {
            var expectedTask = expectedTasks[i];
            var block = res.Missions[i];

            Assert.Equal(expectedTask.Id, block.CombatTaskId);
            Assert.Equal(expectedTask.MissionId, block.MissionId);

            // MissionName uses Mission.ToString()
            Assert.Equal(expectedTask.Mission?.ToString() ?? string.Empty, block.MissionName);

            // SourceDocument fallback
            Assert.Equal(expectedTask.SourceDocument ?? string.Empty, block.SourceDocument);

            // assert: details sorting
            var expectedDetails = expectedTask.CombatTaskDetails
                .OrderBy(l => l.EffectiveAt)
                .ThenBy(l => l.Kind)
                .ThenBy(l => l.FullName)
                .ThenBy(l => l.Id)
                .ToList();

            Assert.Equal(expectedDetails.Count, block.CombatTaskDetails.Count);

            for (var j = 0; j < expectedDetails.Count; j++)
            {
                var e = expectedDetails[j];
                var d = block.CombatTaskDetails[j];

                Assert.Equal(e.Id, d.CombatTaskDetailsId);
                Assert.Equal(e.Kind, d.Kind);
                Assert.Equal(e.EffectiveAt, d.EffectiveAt);
                Assert.Equal(e.PersonId, d.PersonId);
                Assert.Equal(e.Rnokpp, d.Rnokpp);
                Assert.Equal(e.FullName, d.FullName);
                Assert.Equal(e.Rank, d.Rank);
                Assert.Equal(e.Position, d.Position);
                Assert.Equal(e.Weapon, d.Weapon);
                Assert.Equal(e.Callsign, d.Callsign);
            }
        }

        repo.Verify(r => r.GetDocumentAsync(documentId, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_UsesEmptyStrings_WhenMissionOrSourceDocumentIsNull()
    {
        // arrange
        var documentId = Guid.NewGuid();

        var repo = new Mock<ICombatTaskRepository>(MockBehavior.Strict);

        var doc = NewDocument(documentId);

        // Task without Mission loaded + without SourceDocument
        var task = NewTask(doc,
            combatTaskId: Guid.NewGuid(),
            missionId: Guid.Parse("00000000-0000-0000-0000-000000000010"),
            mission: null,
            sourceDocument: null);

        AddDetail(task,
            id: Guid.NewGuid(),
            kind: CombatTaskDetailsKind.Start,
            effectiveAt: new DateOnly(2026, 02, 10),
            fullName: "P");

        doc.CombatTasks.Add(task);

        repo.Setup(r => r.GetDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var handler = new GetCombatTaskDetailsByDocumentIdQueryHandler(repo.Object);

        // act
        var res = await handler.HandleAsync(new GetCombatTaskDetailsByDocumentIdQuery(documentId));

        // assert
        Assert.NotNull(res);
        Assert.Single(res!.Missions);

        var block = res.Missions[0];
        Assert.Equal(string.Empty, block.MissionName);
        Assert.Equal(string.Empty, block.SourceDocument);

        repo.Verify(r => r.GetDocumentAsync(documentId, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static CombatTaskDocument NewDocument(Guid id)
    => new()
    {
        Id = id,
        OrderTitle = "Наказ №1",
        Description = "D",
        Status = DocumentStatus.Active,
        RecordedAt = new DateOnly(2026, 02, 10),
        CreatedBy = "seed",
        CreatedAtUtc = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc)
    };

    private static Mission NewMission(Guid id, string area, string target)
        => new()
        {
            Id = id,
            PositionArea = area,
            NamePoint = null,
            TypeDrone = "UAS",
            Target = target,
            MissionMode = MissionMode.Day,
            CreatedAt = new DateOnly(2026, 02, 01),
            ClosedAt = null
        };

    private static CombatTask NewTask(
        CombatTaskDocument doc,
        Guid combatTaskId,
        Guid missionId,
        Mission? mission,
        string? sourceDocument)
        => new()
        {
            Id = combatTaskId,
            CombatTaskDocumentId = doc.Id,
            CombatTaskDocument = doc,
            MissionId = missionId,
            Mission = mission,
            SourceDocument = sourceDocument!
        };

    private static void AddDetail(
        CombatTask task,
        Guid id,
        CombatTaskDetailsKind kind,
        DateOnly effectiveAt,
        string fullName)
    {
        task.CombatTaskDetails.Add(new CombatTaskDetails
        {
            Id = id,
            CombatTaskId = task.Id,
            CombatTask = task,

            Kind = kind,
            EffectiveAt = effectiveAt,

            PersonId = Guid.NewGuid(),
            Rnokpp = "1234567890",
            FullName = fullName,

            Rank = "R",
            Position = "P",
            Weapon = "W",
            Callsign = "C"
        });
    }
}
