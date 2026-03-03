//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskDetailsByDocumentIdQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.Handlers.CombatTasks;
using eRaven.Application.Queries.CombatTasks;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Tests.Application.Handlers.CombatTasks;

public sealed class GetCombatTaskDetailsByDocumentIdQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Throws_WhenDocumentNotFound()
    {
        // Arrange
        var repo = new FakeRepo { Document = null };
        var sut = new GetCombatTaskDetailsByDocumentIdQueryHandler(repo);

        var query = new GetCombatTaskDetailsByDocumentIdQuery(
            DocumentId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(query));

        // Assert
        Assert.Contains("Документ", ex.Message);
        Assert.Contains("не знайдено", ex.Message);
        Assert.Equal(1, repo.GetDocumentCallCount);
        Assert.Equal(query.DocumentId, repo.LastDocumentId);
    }

    [Fact]
    public async Task HandleAsync_MapsAndOrders_MissionsAndDetails_AndPassesCancellationToken()
    {
        // Arrange
        var docId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // tasks are intentionally unsorted
        var missionIdA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var missionIdB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var taskAId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        var taskBId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

        var taskA = new CombatTask
        {
            Id = taskAId,
            CombatTaskDocumentId = docId,
            MissionId = missionIdB,
            Mission = null,
            SourceDocument = "SRC-B",
            CombatTaskDetails =
            [
                new CombatTaskDetails
                {
                    Id = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111"),
                    Kind = CombatTaskDetailsKind.End,
                    EffectiveAt = new DateOnly(2026, 3, 1),
                    PersonId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Rnokpp = "0000000001",
                    FullName = "Богдан",
                    Rank = "r",
                    Position = "p",
                    Weapon = "w",
                    Callsign = "c",
                },
            ]
        };

        // Details order should be: by date asc, then kind asc (Start before End), then FullName asc, then Id
        var taskB = new CombatTask
        {
            Id = taskBId,
            CombatTaskDocumentId = docId,
            MissionId = missionIdA,
            Mission = null,
            SourceDocument = null!, // simulate legacy/null data from DB
            CombatTaskDetails =
            [
                new CombatTaskDetails
                {
                    Id = Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222"),
                    Kind = CombatTaskDetailsKind.End,
                    EffectiveAt = new DateOnly(2026, 3, 1),
                    PersonId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    Rnokpp = "0000000002",
                    FullName = "Ярослав",
                },
                new CombatTaskDetails
                {
                    Id = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"),
                    Kind = CombatTaskDetailsKind.Start,
                    EffectiveAt = new DateOnly(2026, 3, 1),
                    PersonId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                    Rnokpp = "0000000003",
                    FullName = "Андрій",
                },
                new CombatTaskDetails
                {
                    Id = Guid.Parse("aaaaaaaa-3333-3333-3333-333333333333"),
                    Kind = CombatTaskDetailsKind.Start,
                    EffectiveAt = new DateOnly(2026, 2, 28),
                    PersonId = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                    Rnokpp = "0000000004",
                    FullName = "Євген",
                },
                new CombatTaskDetails
                {
                    Id = Guid.Parse("aaaaaaaa-4444-4444-4444-444444444444"),
                    Kind = CombatTaskDetailsKind.Start,
                    EffectiveAt = new DateOnly(2026, 3, 1),
                    PersonId = Guid.Parse("00000000-0000-0000-0000-000000000005"),
                    Rnokpp = "0000000005",
                    FullName = "Андрій", // same FullName, different Id to verify tie-breaker by Id
                },
            ]
        };

        var document = new CombatTaskDocument
        {
            Id = docId,
            Status = DocumentStatus.Canceled,
            OrderTitle = "Наказ №7",
            Description = null,
            RecordedAt = new DateOnly(2026, 3, 2),
            CanceledReason = "Причина",
            CombatTasks = [taskA, taskB],
        };

        var repo = new FakeRepo { Document = document };
        var sut = new GetCombatTaskDetailsByDocumentIdQueryHandler(repo);

        using var cts = new CancellationTokenSource();
        var query = new GetCombatTaskDetailsByDocumentIdQuery(docId);

        // Act
        var resultMaybe = await sut.HandleAsync(query, cts.Token);

        // Assert
        Assert.Equal(1, repo.GetDocumentCallCount);
        Assert.Equal(docId, repo.LastDocumentId);
        Assert.Equal(cts.Token, repo.LastToken);

        Assert.NotNull(resultMaybe);
        var result = resultMaybe!;

        Assert.Equal(docId, result.DocumentId);
        Assert.Equal("Наказ №7", result.DocumentName);
        Assert.Null(result.Description);
        Assert.Equal(DocumentStatusDto.Canceled, result.Status);
        Assert.Equal(new DateOnly(2026, 3, 2), result.RecordedAt);

        // Missions order: by MissionId asc (missionIdA first), then by CombatTaskId
        Assert.Equal(2, result.Missions.Count);
        Assert.Equal(taskBId, result.Missions[0].CombatTaskId);
        Assert.Equal(missionIdA, result.Missions[0].MissionId);
        Assert.Equal(string.Empty, result.Missions[0].MissionName);
        Assert.Equal(string.Empty, result.Missions[0].SourceDocument); // null -> empty

        Assert.Equal(taskAId, result.Missions[1].CombatTaskId);
        Assert.Equal(missionIdB, result.Missions[1].MissionId);
        Assert.Equal("SRC-B", result.Missions[1].SourceDocument);

        // Details order inside missionIdA (taskB):
        // 2026-02-28 Start (Євген)
        // 2026-03-01 Start (Андрій, Id 1111...)
        // 2026-03-01 Start (Андрій, Id 4444...)
        // 2026-03-01 End   (Ярослав)
        var lines = result.Missions[0].CombatTaskDetails;
        Assert.Equal(4, lines.Count);

        Assert.Equal(new DateOnly(2026, 2, 28), lines[0].EffectiveAt);
        Assert.Equal(CombatTaskDetailsKindDto.Start, lines[0].Kind);
        Assert.Equal("Євген", lines[0].FullName);

        Assert.Equal(new DateOnly(2026, 3, 1), lines[1].EffectiveAt);
        Assert.Equal(CombatTaskDetailsKindDto.Start, lines[1].Kind);
        Assert.Equal("Андрій", lines[1].FullName);
        Assert.Equal(Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"), lines[1].CombatTaskDetailsId);

        Assert.Equal(new DateOnly(2026, 3, 1), lines[2].EffectiveAt);
        Assert.Equal(CombatTaskDetailsKindDto.Start, lines[2].Kind);
        Assert.Equal("Андрій", lines[2].FullName);
        Assert.Equal(Guid.Parse("aaaaaaaa-4444-4444-4444-444444444444"), lines[2].CombatTaskDetailsId);

        Assert.Equal(new DateOnly(2026, 3, 1), lines[3].EffectiveAt);
        Assert.Equal(CombatTaskDetailsKindDto.End, lines[3].Kind);
        Assert.Equal("Ярослав", lines[3].FullName);
        Assert.Equal("0000000002", lines[3].Rnokpp);

        // smoke-check mapping for other snapshot fields on missionIdB (taskA)
        var bLine = result.Missions[1].CombatTaskDetails.Single();
        Assert.Equal("Богдан", bLine.FullName);
        Assert.Equal("r", bLine.Rank);
        Assert.Equal("p", bLine.Position);
        Assert.Equal("w", bLine.Weapon);
        Assert.Equal("c", bLine.Callsign);
    }



    [Fact]
    public async Task HandleAsync_OrdersSameDay_StartThenEnd_WhenInputIsStartThenEnd()
    {
        // Arrange
        var docId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var missionId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var taskId = Guid.Parse("22222222-0000-0000-0000-000000000001");

        var startId = Guid.Parse("22222222-1111-1111-1111-111111111111");
        var endId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var d = new DateOnly(2026, 3, 1);

        var task = new CombatTask
        {
            Id = taskId,
            CombatTaskDocumentId = docId,
            MissionId = missionId,
            SourceDocument = "SRC",
            CombatTaskDetails =
            [
                new CombatTaskDetails
                {
                    Id = startId,
                    Kind = CombatTaskDetailsKind.Start,
                    EffectiveAt = d,
                    PersonId = Guid.Parse("00000000-0000-0000-0000-000000000010"),
                    Rnokpp = "0000000010",
                    FullName = "Іван",
                },
                new CombatTaskDetails
                {
                    Id = endId,
                    Kind = CombatTaskDetailsKind.End,
                    EffectiveAt = d,
                    PersonId = Guid.Parse("00000000-0000-0000-0000-000000000010"),
                    Rnokpp = "0000000010",
                    FullName = "Іван",
                },
            ]
        };

        var document = new CombatTaskDocument
        {
            Id = docId,
            Status = DocumentStatus.Active,
            OrderTitle = "Doc",
            RecordedAt = d,
            CombatTasks = [task],
        };

        var repo = new FakeRepo { Document = document };
        var sut = new GetCombatTaskDetailsByDocumentIdQueryHandler(repo);

        // Act
        var result = await sut.HandleAsync(new GetCombatTaskDetailsByDocumentIdQuery(docId));

        // Assert
        Assert.NotNull(result);
        var lines = result!.Missions.Single().CombatTaskDetails;
        Assert.Equal(2, lines.Count);

        Assert.Equal(CombatTaskDetailsKindDto.Start, lines[0].Kind);
        Assert.Equal(startId, lines[0].CombatTaskDetailsId);

        Assert.Equal(CombatTaskDetailsKindDto.End, lines[1].Kind);
        Assert.Equal(endId, lines[1].CombatTaskDetailsId);
    }

    [Fact]
    public async Task HandleAsync_OrdersSameDay_StartThenEnd_WhenInputIsEndThenStart()
    {
        // Arrange
        var docId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var missionId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var taskId = Guid.Parse("33333333-0000-0000-0000-000000000001");

        var startId = Guid.Parse("33333333-1111-1111-1111-111111111111");
        var endId = Guid.Parse("33333333-2222-2222-2222-222222222222");

        var d = new DateOnly(2026, 3, 1);

        var task = new CombatTask
        {
            Id = taskId,
            CombatTaskDocumentId = docId,
            MissionId = missionId,
            SourceDocument = "SRC",
            CombatTaskDetails =
            [
                new CombatTaskDetails
                {
                    Id = endId,
                    Kind = CombatTaskDetailsKind.End,
                    EffectiveAt = d,
                    PersonId = Guid.Parse("00000000-0000-0000-0000-000000000011"),
                    Rnokpp = "0000000011",
                    FullName = "Іван",
                },
                new CombatTaskDetails
                {
                    Id = startId,
                    Kind = CombatTaskDetailsKind.Start,
                    EffectiveAt = d,
                    PersonId = Guid.Parse("00000000-0000-0000-0000-000000000011"),
                    Rnokpp = "0000000011",
                    FullName = "Іван",
                },
            ]
        };

        var document = new CombatTaskDocument
        {
            Id = docId,
            Status = DocumentStatus.Active,
            OrderTitle = "Doc",
            RecordedAt = d,
            CombatTasks = [task],
        };

        var repo = new FakeRepo { Document = document };
        var sut = new GetCombatTaskDetailsByDocumentIdQueryHandler(repo);

        // Act
        var result = await sut.HandleAsync(new GetCombatTaskDetailsByDocumentIdQuery(docId));

        // Assert
        Assert.NotNull(result);
        var lines = result!.Missions.Single().CombatTaskDetails;
        Assert.Equal(2, lines.Count);

        // Deterministic ordering: EffectiveAt asc, then Kind asc (Start before End).
        Assert.Equal(CombatTaskDetailsKindDto.Start, lines[0].Kind);
        Assert.Equal(startId, lines[0].CombatTaskDetailsId);

        Assert.Equal(CombatTaskDetailsKindDto.End, lines[1].Kind);
        Assert.Equal(endId, lines[1].CombatTaskDetailsId);
    }

    private sealed class FakeRepo : ICombatTaskRepository
    {
        public CombatTaskDocument? Document { get; init; }

        public int GetDocumentCallCount { get; private set; }
        public Guid LastDocumentId { get; private set; }
        public CancellationToken LastToken { get; private set; }

        public Task<IReadOnlyList<Guid>> GetDocumentMissionIdsAsync(Guid documentId, CancellationToken ct = default)
            => throw new NotSupportedException("Not needed for handler tests");

        public Task<CombatTaskDocument> GetDocumentAsync(Guid documentId, CancellationToken ct = default)
        {
            GetDocumentCallCount++;
            LastDocumentId = documentId;
            LastToken = ct;

            // interface is non-nullable, but we simulate "not found" via null! in tests
            return Task.FromResult(Document ?? null!);
        }

        public Task<Guid> CreateCombatTaskAsync(Guid documentId, Guid missionId, string sourceDocument, IReadOnlyCollection<CombatTaskDetails> combatTaskDetails, CancellationToken ct = default)
            => throw new NotSupportedException("Not needed for handler tests");

        public Task<Guid> UpsertCombatTaskAsync(Guid documentId, Guid missionId, string sourceDocument, IReadOnlyCollection<CombatTaskDetails> combatTaskDetails, CancellationToken ct = default)
            => throw new NotSupportedException("Not needed for handler tests");

        public Task DeleteCombatTaskAsync(Guid documentId, Guid combatTaskId, CancellationToken ct = default)
            => throw new NotSupportedException("Not needed for handler tests");
    }
}
