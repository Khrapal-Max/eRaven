//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Commands.CombatTasks;
using eRaven.Application.DTOs.CombatTasks;
using eRaven.Application.Handlers.CombatTasks;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Moq;

namespace eRaven.Tests.Application.Handlers.CombatTasks;

/// <summary>
/// Тести для <see cref="CreateCombatTaskCommandHandler"/>.
///
/// <para>
/// Фіксуємо поведінку:
/// <list type="bullet">
/// <item><description>Create path: trim + de-dup по бізнес-ключу + ApplyFacts отримує persistedRows.</description></item>
/// <item><description>Upsert path: merge збереження Id існуючого рядка + fallback snapshot з existing + no PersonRepo.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class CreateCombatTaskCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatePath_TrimsSource_DedupsByBusinessKey_AndAppliesPersistedRows()
    {
        // arrange
        var repo = new Mock<ICombatTaskRepository>(MockBehavior.Strict);
        var timesheets = new Mock<ITimesheetAggregateRepository>(MockBehavior.Strict);
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var combatTaskId = Guid.NewGuid();

        var personId = Guid.NewGuid();

        // Document without mission block => Create path.
        var document = NewDocument(documentId, combatTasks: []);

        repo.Setup(x => x.GetDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Two incoming rows with SAME business key => should dedup into 1 persisted row:
        // key = PersonId + Kind + EffectiveAt
        var firstRowId = Guid.NewGuid();
        var effectiveAt = new DateOnly(2026, 02, 10);

        var cmd = new CreateCombatTaskCommand(
            DocumentId: documentId,
            MissionId: missionId,
            SourceDocument: "  SRC  ",
            CombatTaskDetails:
            [
                NewDtoRow(firstRowId, CombatTaskDetailsKind.Start, effectiveAt, personId,
                    rnokpp: " 111 ", fullName: "  A  ", rank: null, position: null, weapon: null, callsign: null),

                // duplicate key, should "win" snapshot but keep existing Id (firstRowId)
                NewDtoRow(Guid.NewGuid(), CombatTaskDetailsKind.Start, effectiveAt, personId,
                    rnokpp: " 111 ", fullName: "  B  ", rank: null, position: null, weapon: null, callsign: null)
            ],
            Author: "  admin  ",
            NowUtc: now);

        IReadOnlyCollection<CombatTaskDetails>? createRows = null;
        IReadOnlyCollection<CombatTaskDetails>? appliedRows = null;

        repo.Setup(x => x.CreateCombatTaskAsync(
                documentId,
                missionId,
                "SRC",
                It.IsAny<IReadOnlyCollection<CombatTaskDetails>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, string, IReadOnlyCollection<CombatTaskDetails>, CancellationToken>((_, _, _, rows, _) =>
            {
                createRows = rows;
            })
            .ReturnsAsync(combatTaskId);

        // Since snapshot fields (Rank/Position/Weapon/Callsign) are missing, handler will ask PersonRepo.
        // We don't rely on concrete PersonDetailsDto ctor here => return null, but verify call happened.
        persons.Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((eRaven.Application.DTOs.Person.PersonDetailsDto?)null);

        timesheets.Setup(x => x.ApplyCombatTaskFactsAsync(
                documentId,
                missionId,
                It.IsAny<IReadOnlyCollection<CombatTaskDetails>>(),
                "admin",
                now,
                It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, IReadOnlyCollection<CombatTaskDetails>, string, DateTime, CancellationToken>((_, _, rows, _, _, _) =>
            {
                appliedRows = rows;
            })
            .Returns(Task.CompletedTask);

        var handler = new CreateCombatTaskCommandHandler(repo.Object, timesheets.Object, persons.Object);

        // act
        var resId = await handler.HandleAsync(cmd);

        // assert
        Assert.Equal(combatTaskId, resId);

        Assert.NotNull(createRows);
        Assert.Single(createRows!);

        var persisted = createRows!.Single();

        // Dedup: Id should be from the first row, but snapshot should be from the second ("wins")
        Assert.Equal(firstRowId, persisted.Id);
        Assert.Equal(personId, persisted.PersonId);
        Assert.Equal(CombatTaskDetailsKind.Start, persisted.Kind);
        Assert.Equal(effectiveAt, persisted.EffectiveAt);

        Assert.Equal("111", persisted.Rnokpp);
        Assert.Equal("B", persisted.FullName);

        // Apply facts MUST use persistedRows (same content; can be same instance or equivalent)
        Assert.NotNull(appliedRows);
        Assert.Single(appliedRows!);

        var applied = appliedRows!.Single();
        Assert.Equal(persisted.Id, applied.Id);
        Assert.Equal(persisted.PersonId, applied.PersonId);
        Assert.Equal(persisted.Kind, applied.Kind);
        Assert.Equal(persisted.EffectiveAt, applied.EffectiveAt);
        Assert.Equal(persisted.Rnokpp, applied.Rnokpp);
        Assert.Equal(persisted.FullName, applied.FullName);

        repo.VerifyAll();
        persons.VerifyAll();
        timesheets.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_UpsertPath_MergesByBusinessKey_PreservesExistingId_UsesExistingSnapshot_AndDoesNotQueryPersons()
    {
        // arrange
        var repo = new Mock<ICombatTaskRepository>(MockBehavior.Strict);
        var timesheets = new Mock<ITimesheetAggregateRepository>(MockBehavior.Strict);
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var combatTaskId = Guid.NewGuid();

        var personId = Guid.NewGuid();
        var effectiveAt = new DateOnly(2026, 02, 10);

        var existingDetailId = Guid.NewGuid();
        var existingTaskId = Guid.NewGuid();

        // Document WITH mission block => Upsert path.
        var existingTask = new CombatTask
        {
            Id = existingTaskId,
            CombatTaskDocumentId = documentId,
            MissionId = missionId,
            SourceDocument = "OLD",
            Mission = null,
            CombatTaskDetails =
            [
                new CombatTaskDetails
                {
                    Id = existingDetailId,
                    CombatTaskId = existingTaskId,
                    CombatTask = null,
                    Kind = CombatTaskDetailsKind.Start,
                    EffectiveAt = effectiveAt,
                    PersonId = personId,
                    Rnokpp = "111",
                    FullName = "Existing Name",
                    Rank = "R",
                    Position = "P",
                    Weapon = "W",
                    Callsign = "C"
                }
            ]
        };

        var document = NewDocument(documentId, combatTasks: [existingTask]);

        repo.Setup(x => x.GetDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Incoming row with same business key but missing snapshot => should be enriched from existing block,
        // merged should preserve existingDetailId, but update FullName from incoming.
        var cmd = new CreateCombatTaskCommand(
            DocumentId: documentId,
            MissionId: missionId,
            SourceDocument: "  SRC-NEW  ",
            CombatTaskDetails:
            [
                NewDtoRow(Guid.Empty, CombatTaskDetailsKind.Start, effectiveAt, personId,
                    rnokpp: "111", fullName: "  Updated Name  ", rank: null, position: null, weapon: null, callsign: null)
            ],
            Author: " duty ",
            NowUtc: now);

        IReadOnlyCollection<CombatTaskDetails>? upsertRows = null;
        IReadOnlyCollection<CombatTaskDetails>? appliedRows = null;

        repo.Setup(x => x.UpsertCombatTaskAsync(
                documentId,
                missionId,
                "SRC-NEW",
                It.IsAny<IReadOnlyCollection<CombatTaskDetails>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, string, IReadOnlyCollection<CombatTaskDetails>, CancellationToken>((_, _, _, rows, _) =>
            {
                upsertRows = rows;
            })
            .ReturnsAsync(combatTaskId);

        // PersonRepo MUST NOT be called: existing block has full snapshot for this person.
        persons.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("PersonRepository must not be called in this scenario."));

        timesheets.Setup(x => x.ApplyCombatTaskFactsAsync(
                documentId,
                missionId,
                It.IsAny<IReadOnlyCollection<CombatTaskDetails>>(),
                "duty",
                now,
                It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, IReadOnlyCollection<CombatTaskDetails>, string, DateTime, CancellationToken>((_, _, rows, _, _, _) =>
            {
                appliedRows = rows;
            })
            .Returns(Task.CompletedTask);

        var handler = new CreateCombatTaskCommandHandler(repo.Object, timesheets.Object, persons.Object);

        // act
        var resId = await handler.HandleAsync(cmd);

        // assert
        Assert.Equal(combatTaskId, resId);

        Assert.NotNull(upsertRows);
        Assert.Single(upsertRows!);

        var persisted = upsertRows!.Single();

        // Merge must preserve existing Id
        Assert.Equal(existingDetailId, persisted.Id);

        // Incoming updates FullName (trimmed), but missing Rank/Position/Weapon/Callsign should come from existing block
        Assert.Equal("Updated Name", persisted.FullName);
        Assert.Equal("111", persisted.Rnokpp);

        Assert.Equal("R", persisted.Rank);
        Assert.Equal("P", persisted.Position);
        Assert.Equal("W", persisted.Weapon);
        Assert.Equal("C", persisted.Callsign);

        // Apply facts MUST use merged persistedRows
        Assert.NotNull(appliedRows);
        Assert.Single(appliedRows!);

        var applied = appliedRows!.Single();
        Assert.Equal(existingDetailId, applied.Id);
        Assert.Equal("Updated Name", applied.FullName);
        Assert.Equal("R", applied.Rank);

        repo.VerifyAll();
        timesheets.VerifyAll();
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static CombatTaskDocument NewDocument(Guid documentId, IReadOnlyCollection<CombatTask> combatTasks)
        => new()
        {
            Id = documentId,
            OrderTitle = "Doc",
            Description = null,
            Status = DocumentStatus.Active,
            RecordedAt = new DateOnly(2026, 02, 10),
            CanceledReason = null,
            CombatTasks = [.. combatTasks]
        };

    private static CombatTaskDetailsDto NewDtoRow(
        Guid detailsId,
        CombatTaskDetailsKind kind,
        DateOnly effectiveAt,
        Guid personId,
        string rnokpp,
        string fullName,
        string? rank,
        string? position,
        string? weapon,
        string? callsign)
        => new(
            CombatTaskDetailsId: detailsId,
            Kind: kind,
            EffectiveAt: effectiveAt,
            PersonId: personId,
            Rnokpp: rnokpp,
            Rank: rank,
            FullName: fullName,
            Position: position,
            Weapon: weapon,
            Callsign: callsign);
}
