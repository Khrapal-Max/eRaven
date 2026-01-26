//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskPlanDocumentRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class CombatTaskPlanDocumentRepositoryTests
{
    private static CombatTaskPlanLineInputDto StartLine(
        Guid personId,
        DateOnly actionDate,
        string rnokpp = "1234567890",
        string fullName = "Тест Т.Т.",
        string area = "Район-1",
        string group = "ГРУПА-1",
        string goal = "Goal-1",
        CombatTaskMode mode = CombatTaskMode.Day,
        string? assetType = null,
        bool isActual = true,
        string? note = null)
        => new(
            Kind: CombatTaskPlanLineKind.Start,
            PersonId: personId,
            ActionDate: actionDate,
            RNOKPP: rnokpp,
            FullName: fullName,
            Rank: "Сержант",
            Position: "Оператор",
            Weapon: "АК",
            Callsign: "FOX",
            PositionalArea: area,
            GroupName: group,
            AssetType: assetType,
            Mode: mode,
            Goal: goal,
            IsActual: isActual,
            Note: note,
            AssignmentId: null
        );

    private static CombatTaskPlanLineInputDto EndLine(
        Guid personId,
        DateOnly actionDate,
        Guid? assignmentId = null,
        string rnokpp = "1234567890",
        string fullName = "Тест Т.Т.",
        string area = "Район-1",
        string group = "ГРУПА-1",
        string goal = "Goal-1",
        CombatTaskMode mode = CombatTaskMode.Day,
        string? assetType = null,
        bool isActual = true,
        string? note = null)
        => new(
            Kind: CombatTaskPlanLineKind.End,
            PersonId: personId,
            ActionDate: actionDate,
            RNOKPP: rnokpp,
            FullName: fullName,
            Rank: "Сержант",
            Position: "Оператор",
            Weapon: "АК",
            Callsign: "FOX",
            PositionalArea: area,
            GroupName: group,
            AssetType: assetType,
            Mode: mode,
            Goal: goal,
            IsActual: isActual,
            Note: note,
            AssignmentId: assignmentId
        );

    // Draft не створює Lines — тому для тестів Post/Cancel ми додаємо Lines напряму через DbContext.
    private static async Task<Guid> AddLineToDocumentAsync(SqliteTestDb testDb, Guid docId, CombatTaskPlanLineInputDto input)
    {
        await using var db = await testDb.Factory.CreateDbContextAsync();

        var lineId = Guid.NewGuid();

        db.Set<CombatTaskPlanLine>().Add(new CombatTaskPlanLine
        {
            Id = lineId,
            DocumentId = docId,
            Kind = input.Kind,
            PersonId = input.PersonId,
            AssignmentId = input.AssignmentId ?? Guid.Empty, // важливо: null => Guid.Empty (сигнал "auto resolve")

            ActionDate = input.ActionDate,

            RNOKPP = input.RNOKPP,
            FullName = input.FullName,
            Rank = input.Rank,
            Position = input.Position,
            Weapon = input.Weapon,
            Callsign = input.Callsign,

            PositionalArea = input.PositionalArea,
            GroupName = input.GroupName,
            AssetType = input.AssetType,
            Mode = input.Mode,
            Goal = input.Goal,
            IsActual = input.IsActual,
            Note = input.Note
        });

        await db.SaveChangesAsync();
        return lineId;
    }

    /// <summary>
    /// Створення чернетки: створюється лише "шапка" документа (без Lines), статус Draft.
    /// </summary>
    [Fact]
    public async Task CreateDraftAsync_Creates_DraftDocument_HeaderOnly()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskPlanDocumentRepository(testDb.Factory);

        var nowUtc = DateTime.UtcNow;

        var docId = await repo.CreateDraftAsync(
            recordedAt: new DateOnly(2026, 1, 1),
            planningDate: new DateOnly(2026, 1, 1),
            planningDocTitle: "План №1",
            author: "test",
            nowUtc: nowUtc);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        var doc = await db.Set<CombatTaskPlanDocument>()
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == docId);

        Assert.Equal(CombatTaskPlanDocumentStatus.Draft, doc.Status);
        Assert.Equal("План №1", doc.PlanningDocTitle);
        Assert.Empty(doc.Lines);
    }

    /// <summary>
    /// Проведення документа зі Start-рядком: створює Assignment, проставляє AssignmentId у рядок, документ -> Posted.
    /// </summary>
    [Fact]
    public async Task PostAsync_StartLine_CreatesAssignment_AndPostsDocument()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskPlanDocumentRepository(testDb.Factory);

        var personId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var docId = await repo.CreateDraftAsync(
            recordedAt: new DateOnly(2026, 1, 1),
            planningDate: new DateOnly(2026, 1, 1),
            planningDocTitle: "План №1",
            author: "test",
            nowUtc: nowUtc);

        await AddLineToDocumentAsync(testDb, docId,
            StartLine(personId, new DateOnly(2026, 1, 1), goal: "Розвідка", area: "A", group: "G"));

        await repo.PostAsync(docId, "poster", nowUtc);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        var doc = await db.Set<CombatTaskPlanDocument>()
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == docId);

        Assert.Equal(CombatTaskPlanDocumentStatus.Posted, doc.Status);

        var line = doc.Lines.Single();
        Assert.Equal(CombatTaskPlanLineKind.Start, line.Kind);
        Assert.NotEqual(Guid.Empty, line.AssignmentId);

        var a = await db.Set<CombatTaskAssignment>()
            .SingleAsync(x => x.Id == line.AssignmentId);

        Assert.Equal(personId, a.PersonId);
        Assert.Equal(new DateOnly(2026, 1, 1), a.StartedAt);
        Assert.Null(a.EndedAt);
        Assert.Equal(docId, a.StartDocumentId);
        Assert.Null(a.EndDocumentId);

        Assert.Equal(doc.PlanningDate, a.PlanningDate);
        Assert.Equal(doc.PlanningDocTitle, a.PlanningDocTitle);

        Assert.Equal("A", a.PositionalArea);
        Assert.Equal("G", a.GroupName);
        Assert.Equal("Розвідка", a.Goal);

        Assert.True(a.IsActual);
    }

    /// <summary>
    /// End тим же днем закриває активне завдання, після чого Start тим же днем дозволений (2 Assignment по людині).
    /// </summary>
    [Fact]
    public async Task PostAsync_EndLine_ClosesOpenAssignment_AndAllowsNewStartSameDay()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskPlanDocumentRepository(testDb.Factory);

        var personId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        // 1) Post старт
        var startDocId = await repo.CreateDraftAsync(
            recordedAt: new DateOnly(2026, 1, 1),
            planningDate: new DateOnly(2026, 1, 1),
            planningDocTitle: "Старт №1",
            author: "test",
            nowUtc: nowUtc);

        await AddLineToDocumentAsync(testDb, startDocId,
            StartLine(personId, new DateOnly(2026, 1, 1), goal: "Goal-Start"));

        await repo.PostAsync(startDocId, "poster", nowUtc);

        // 2) Закриття тим же днем (AssignmentId = null => Guid.Empty => auto resolve open)
        var endDocId = await repo.CreateDraftAsync(
            recordedAt: new DateOnly(2026, 1, 1),
            planningDate: new DateOnly(2026, 1, 1),
            planningDocTitle: "Кінець №1",
            author: "test",
            nowUtc: nowUtc);

        await AddLineToDocumentAsync(testDb, endDocId,
            EndLine(personId, new DateOnly(2026, 1, 1), assignmentId: null));

        await repo.PostAsync(endDocId, "poster", nowUtc);

        // 3) Новий старт тим же днем — має бути дозволено
        var startDoc2Id = await repo.CreateDraftAsync(
            recordedAt: new DateOnly(2026, 1, 1),
            planningDate: new DateOnly(2026, 1, 1),
            planningDocTitle: "Старт №2",
            author: "test",
            nowUtc: nowUtc);

        await AddLineToDocumentAsync(testDb, startDoc2Id,
            StartLine(personId, new DateOnly(2026, 1, 1), goal: "Goal-Start-2"));

        await repo.PostAsync(startDoc2Id, "poster", nowUtc);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        var all = await db.Set<CombatTaskAssignment>()
            .Where(x => x.PersonId == personId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync();

        Assert.Equal(2, all.Count);

        var first = all[0];
        Assert.Equal(new DateOnly(2026, 1, 1), first.StartedAt);
        Assert.Equal(new DateOnly(2026, 1, 1), first.EndedAt);
        Assert.Equal(endDocId, first.EndDocumentId);
        Assert.False(first.IsActual);

        var second = all[1];
        Assert.Equal(new DateOnly(2026, 1, 1), second.StartedAt);
        Assert.Null(second.EndedAt);
        Assert.True(second.IsActual);
    }

    /// <summary>
    /// Якщо вже є активне завдання, то проведення нового Start-документа повинно кинути помилку.
    /// </summary>
    [Fact]
    public async Task PostAsync_Start_WhenOpenExists_Throws()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskPlanDocumentRepository(testDb.Factory);

        var personId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var doc1 = await repo.CreateDraftAsync(
            recordedAt: new DateOnly(2026, 1, 1),
            planningDate: new DateOnly(2026, 1, 1),
            planningDocTitle: "Doc1",
            author: "test",
            nowUtc: nowUtc);

        await AddLineToDocumentAsync(testDb, doc1, StartLine(personId, new DateOnly(2026, 1, 1)));
        await repo.PostAsync(doc1, "poster", nowUtc);

        var doc2 = await repo.CreateDraftAsync(
            recordedAt: new DateOnly(2026, 1, 2),
            planningDate: new DateOnly(2026, 1, 2),
            planningDocTitle: "Doc2",
            author: "test",
            nowUtc: nowUtc);

        await AddLineToDocumentAsync(testDb, doc2, StartLine(personId, new DateOnly(2026, 1, 2)));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repo.PostAsync(doc2, "poster", nowUtc));

        Assert.Contains("вже має активне завдання", ex.Message);
    }

    /// <summary>
    /// Відміна: Draft можна відмінити; Posted — не можна (повинна бути помилка).
    /// </summary>
    [Fact]
    public async Task CancelAsync_Draft_Cancels_ButPosted_Throws()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskPlanDocumentRepository(testDb.Factory);

        var nowUtc = DateTime.UtcNow;

        // Draft cancel OK
        var draftId = await repo.CreateDraftAsync(
            recordedAt: new DateOnly(2026, 1, 1),
            planningDate: new DateOnly(2026, 1, 1),
            planningDocTitle: "Draft",
            author: "test",
            nowUtc: nowUtc);

        await repo.CancelAsync(draftId, "Помилка вводу", "canceler", nowUtc);

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var doc = await db.Set<CombatTaskPlanDocument>().SingleAsync(x => x.Id == draftId);
            Assert.Equal(CombatTaskPlanDocumentStatus.Canceled, doc.Status);
            Assert.Equal("Помилка вводу", doc.CanceledReason);
        }

        // Posted cancel -> throw
        var postedId = await repo.CreateDraftAsync(
            recordedAt: new DateOnly(2026, 1, 2),
            planningDate: new DateOnly(2026, 1, 2),
            planningDocTitle: "Posted",
            author: "test",
            nowUtc: nowUtc);

        await AddLineToDocumentAsync(testDb, postedId,
            StartLine(Guid.NewGuid(), new DateOnly(2026, 1, 2)));

        await repo.PostAsync(postedId, "poster", nowUtc);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repo.CancelAsync(postedId, "Ні", "canceler", nowUtc));
    }
}
