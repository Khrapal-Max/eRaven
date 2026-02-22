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
/// <para>Фіксуємо ключову бізнес-поведінку:</para>
/// <list type="bullet">
/// <item><description>якщо місії в документі немає — викликаємо <c>CreateCombatTaskAsync</c>;</description></item>
/// <item><description>якщо місія вже є — викликаємо <c>UpsertCombatTaskAsync</c> (replace-all rows);</description></item>
/// <item><description>enrich snapshot з існуючих рядків місії (fallback), без зайвих запитів у PersonRepo;</description></item>
/// <item><description>завжди викликаємо <c>ApplyCombatTaskFactsAsync</c> по “current truth” (incoming rows);</description></item>
/// <item><description>trim для <c>SourceDocument</c>/<c>Author</c>/<c>Rnokpp</c>/<c>FullName</c>;</description></item>
/// <item><description>валідації: пустий/NULL список рядків, null-рядок, пусті ключові поля.</description></item>
/// </list>
/// </summary>
public sealed class CreateCombatTaskCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatesCombatTask_WhenMissionDoesNotExist_PassesTrimmedArgs_AndAppliesFacts()
    {
        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var nowUtc = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var author = "  admin  ";
        var source = "  SRC-1  ";

        var personId = Guid.NewGuid();

        var document = NewDocument(docId, status: DocumentStatus.Active);
        // no CombatTasks => missionEntity == null (create path)

        IReadOnlyCollection<CombatTaskDetails>? createRows = null;
        IReadOnlyCollection<CombatTaskDetails>? appliedRows = null;

        var createdTaskId = Guid.NewGuid();

        var repo = new Mock<ICombatTaskRepository>(MockBehavior.Strict);
        repo.Setup(x => x.GetDocumentAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        repo.Setup(x => x.CreateCombatTaskAsync(
                docId,
                missionId,
                "SRC-1",
                It.IsAny<IReadOnlyCollection<CombatTaskDetails>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, string, IReadOnlyCollection<CombatTaskDetails>, CancellationToken>((_, _, _, rows, _) =>
                createRows = rows)
            .ReturnsAsync(createdTaskId);

        // Ensure upsert is NOT used in this test (Strict)
        // repo.Setup(...) for Upsert not needed.

        var timesheets = new Mock<ITimesheetAggregateRepository>(MockBehavior.Strict);
        timesheets.Setup(x => x.ApplyCombatTaskFactsAsync(
                docId,
                "DOC1",
                missionId,
                It.IsAny<IReadOnlyCollection<CombatTaskDetails>>(),
                "admin",
                nowUtc,
                It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, IReadOnlyCollection<CombatTaskDetails>, string, DateTime, CancellationToken>((_, _, rows, _, _, _) =>
                appliedRows = rows)
            .Returns(Task.CompletedTask);

        // PersonRepo must NOT be called here: we send complete snapshot in DTO
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var handler = new CreateCombatTaskCommandHandler(repo.Object, timesheets.Object, persons.Object);

        var cmd = new CreateCombatTaskCommand(
            DocumentId: docId,
            MissionId: missionId,
            SourceDocument: source,
            CombatTaskDetails:
            [
                NewDto(
                    id: Guid.Empty, // force handler to generate
                    kind: CombatTaskDetailsKind.Start,
                    at: new DateOnly(2026, 02, 10),
                    personId: personId,
                    rnokpp: "  1234567890  ",
                    fullName: "  Person A  ",
                    rank: "  R  ",
                    position: "  P  ",
                    weapon: "  W  ",
                    callsign: "  C  ")
            ],
            Author: author,
            NowUtc: nowUtc);

        var resultId = await handler.HandleAsync(cmd);

        Assert.Equal(createdTaskId, resultId);

        Assert.NotNull(createRows);
        Assert.Single(createRows!);

        var row = createRows!.Single();
        Assert.NotEqual(Guid.Empty, row.Id);

        Assert.Equal(personId, row.PersonId);
        Assert.Equal(CombatTaskDetailsKind.Start, row.Kind);
        Assert.Equal(new DateOnly(2026, 02, 10), row.EffectiveAt);

        // Trim on mapping
        Assert.Equal("1234567890", row.Rnokpp);
        Assert.Equal("Person A", row.FullName);
        Assert.Equal("R", row.Rank);
        Assert.Equal("P", row.Position);
        Assert.Equal("W", row.Weapon);
        Assert.Equal("C", row.Callsign);

        // Apply called with the same "truth" rows (same IDs / values)
        Assert.NotNull(appliedRows);
        Assert.Single(appliedRows!);
        Assert.Equal(row.Id, appliedRows!.Single().Id);
        Assert.Equal("1234567890", appliedRows!.Single().Rnokpp);

        repo.VerifyAll();
        timesheets.VerifyAll();
        persons.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_UpsertsCombatTask_WhenMissionExists_EnrichesFromExistingMissionRows_AndDoesNotCallPersonsRepo()
    {
        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var nowUtc = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var personId = Guid.NewGuid();

        // Existing mission entity already in doc
        var existing = NewDetail(
            id: Guid.NewGuid(),
            kind: CombatTaskDetailsKind.Start,
            at: new DateOnly(2026, 02, 10),
            personId: personId,
            rnokpp: "1111111111",
            fullName: "Existing Person",
            rank: "SGT",
            position: "Operator",
            weapon: "Rifle",
            callsign: "FOX");

        var missionEntity = NewCombatTask(
            id: Guid.NewGuid(),
            documentId: docId,
            missionId: missionId,
            sourceDocument: "EXISTING",
            details: [existing]);

        var document = NewDocument(docId, status: DocumentStatus.Active, tasks: [missionEntity]);

        IReadOnlyCollection<CombatTaskDetails>? upsertRows = null;
        IReadOnlyCollection<CombatTaskDetails>? appliedRows = null;

        var repo = new Mock<ICombatTaskRepository>(MockBehavior.Strict);
        repo.Setup(x => x.GetDocumentAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        repo.Setup(x => x.UpsertCombatTaskAsync(
                docId,
                missionId,
                "SRC-NEW",
                It.IsAny<IReadOnlyCollection<CombatTaskDetails>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, string, IReadOnlyCollection<CombatTaskDetails>, CancellationToken>((_, _, _, rows, _) =>
                upsertRows = rows)
            .ReturnsAsync(missionEntity.Id);

        var timesheets = new Mock<ITimesheetAggregateRepository>(MockBehavior.Strict);
        timesheets.Setup(x => x.ApplyCombatTaskFactsAsync(
                docId,
                "DOC1",
                missionId,
                It.IsAny<IReadOnlyCollection<CombatTaskDetails>>(),
                "duty",
                nowUtc,
                It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, IReadOnlyCollection<CombatTaskDetails>, string, DateTime, CancellationToken>((_, _, rows, _, _, _) =>
                appliedRows = rows)
            .Returns(Task.CompletedTask);

        // PersonRepo MUST NOT be called because fallback from existing closes the gaps
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var handler = new CreateCombatTaskCommandHandler(repo.Object, timesheets.Object, persons.Object);

        // Incoming is incomplete (missing snapshot + empty rnokpp/fullname) => must be enriched from existing rows
        var cmd = new CreateCombatTaskCommand(
            DocumentId: docId,
            MissionId: missionId,
            SourceDocument: "  SRC-NEW  ",
            CombatTaskDetails:
            [
                NewDto(
                    id: Guid.Empty,
                    kind: CombatTaskDetailsKind.Start,
                    at: new DateOnly(2026, 02, 10),
                    personId: personId,
                    rnokpp: "   ",
                    fullName: "   ",
                    rank: null,
                    position: null,
                    weapon: null,
                    callsign: null)
            ],
            Author: "  duty  ",
            NowUtc: nowUtc);

        var resultId = await handler.HandleAsync(cmd);
        Assert.Equal(missionEntity.Id, resultId);

        Assert.NotNull(upsertRows);
        Assert.Single(upsertRows!);

        var row = upsertRows!.Single();

        // Enriched from existing mission rows
        Assert.Equal("1111111111", row.Rnokpp);
        Assert.Equal("Existing Person", row.FullName);
        Assert.Equal("SGT", row.Rank);
        Assert.Equal("Operator", row.Position);
        Assert.Equal("Rifle", row.Weapon);
        Assert.Equal("FOX", row.Callsign);

        // Apply called with the same "truth"
        Assert.NotNull(appliedRows);
        Assert.Single(appliedRows!);
        Assert.Equal("1111111111", appliedRows!.Single().Rnokpp);
        Assert.Equal("Existing Person", appliedRows!.Single().FullName);

        repo.VerifyAll();
        timesheets.VerifyAll();
        persons.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_Throws_WhenDetailsNullOrEmpty()
    {
        var repo = new Mock<ICombatTaskRepository>(MockBehavior.Strict);
        var timesheets = new Mock<ITimesheetAggregateRepository>(MockBehavior.Strict);
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var handler = new CreateCombatTaskCommandHandler(repo.Object, timesheets.Object, persons.Object);

        var cmdNull = new CreateCombatTaskCommand(
            DocumentId: Guid.NewGuid(),
            MissionId: Guid.NewGuid(),
            SourceDocument: "SRC",
            CombatTaskDetails: null!, // runtime safety
            Author: "a",
            NowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc));

        var ex1 = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(cmdNull));
        Assert.Contains("Порожній список", ex1.Message, StringComparison.OrdinalIgnoreCase);

        var cmdEmpty = new CreateCombatTaskCommand(
            DocumentId: Guid.NewGuid(),
            MissionId: Guid.NewGuid(),
            SourceDocument: "SRC",
            CombatTaskDetails: [],
            Author: "a",
            NowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc));

        var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(cmdEmpty));
        Assert.Contains("Порожній список", ex2.Message, StringComparison.OrdinalIgnoreCase);

        repo.VerifyAll();
        timesheets.VerifyAll();
        persons.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_Throws_WhenDetailsContainNullRow()
    {
        var repo = new Mock<ICombatTaskRepository>(MockBehavior.Strict);
        var timesheets = new Mock<ITimesheetAggregateRepository>(MockBehavior.Strict);
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var handler = new CreateCombatTaskCommandHandler(repo.Object, timesheets.Object, persons.Object);

        var cmd = new CreateCombatTaskCommand(
            DocumentId: Guid.NewGuid(),
            MissionId: Guid.NewGuid(),
            SourceDocument: "SRC",
            CombatTaskDetails: [null!],
            Author: "a",
            NowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(cmd));
        Assert.Contains("null-рядок", ex.Message, StringComparison.OrdinalIgnoreCase);

        repo.VerifyAll();
        timesheets.VerifyAll();
        persons.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_Throws_WhenPersonIdOrEffectiveAtMissing()
    {
        var repo = new Mock<ICombatTaskRepository>(MockBehavior.Strict);
        var timesheets = new Mock<ITimesheetAggregateRepository>(MockBehavior.Strict);
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var handler = new CreateCombatTaskCommandHandler(repo.Object, timesheets.Object, persons.Object);

        var cmdBadPerson = new CreateCombatTaskCommand(
            DocumentId: Guid.NewGuid(),
            MissionId: Guid.NewGuid(),
            SourceDocument: "SRC",
            CombatTaskDetails:
            [
                NewDto(
                    id: Guid.Empty,
                    kind: CombatTaskDetailsKind.Start,
                    at: new DateOnly(2026, 02, 10),
                    personId: Guid.Empty,
                    rnokpp: "1",
                    fullName: "P",
                    rank: "R",
                    position: "P",
                    weapon: "W",
                    callsign: "C")
            ],
            Author: "a",
            NowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc));

        var ex1 = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(cmdBadPerson));
        Assert.Contains("PersonId", ex1.Message, StringComparison.OrdinalIgnoreCase);

        var cmdBadDate = new CreateCombatTaskCommand(
            DocumentId: Guid.NewGuid(),
            MissionId: Guid.NewGuid(),
            SourceDocument: "SRC",
            CombatTaskDetails:
            [
                NewDto(
                    id: Guid.Empty,
                    kind: CombatTaskDetailsKind.Start,
                    at: default,
                    personId: Guid.NewGuid(),
                    rnokpp: "1",
                    fullName: "P",
                    rank: "R",
                    position: "P",
                    weapon: "W",
                    callsign: "C")
            ],
            Author: "a",
            NowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc));

        var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(cmdBadDate));
        Assert.Contains("EffectiveAt", ex2.Message, StringComparison.OrdinalIgnoreCase);

        repo.VerifyAll();
        timesheets.VerifyAll();
        persons.VerifyAll();
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static CombatTaskDocument NewDocument(Guid id, DocumentStatus status, IReadOnlyCollection<CombatTask>? tasks = null)
        => new()
        {
            Id = id,
            OrderTitle = "Doc",
            Description = "D",
            Status = status,
            RecordedAt = new DateOnly(2026, 02, 10),
            CombatTasks = tasks?.ToList() ?? []
        };

    private static CombatTask NewCombatTask(Guid id, Guid documentId, Guid missionId, string sourceDocument, IReadOnlyCollection<CombatTaskDetails> details)
        => new()
        {
            Id = id,
            CombatTaskDocumentId = documentId,
            MissionId = missionId,
            SourceDocument = sourceDocument,
            CombatTaskDetails = [.. details]
        };

    private static CombatTaskDetails NewDetail(
        Guid id,
        CombatTaskDetailsKind kind,
        DateOnly at,
        Guid personId,
        string rnokpp,
        string fullName,
        string? rank,
        string? position,
        string? weapon,
        string? callsign)
        => new()
        {
            Id = id,
            Kind = kind,
            EffectiveAt = at,
            PersonId = personId,
            Rnokpp = rnokpp,
            FullName = fullName,
            Rank = rank,
            Position = position,
            Weapon = weapon,
            Callsign = callsign
        };

    private static CombatTaskDetailsDto NewDto(
        Guid id,
        CombatTaskDetailsKind kind,
        DateOnly at,
        Guid personId,
        string rnokpp,
        string fullName,
        string? rank,
        string? position,
        string? weapon,
        string? callsign)
        => new(
            CombatTaskDetailsId: id,
            Kind: kind,
            EffectiveAt: at,
            PersonId: personId,
            Rnokpp: rnokpp,
            Rank: rank,
            FullName: fullName,
            Position: position,
            Weapon: weapon,
            Callsign: callsign);
}
