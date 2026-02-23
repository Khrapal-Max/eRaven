//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDocumentRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

/// <summary>
/// Тести для <see cref="CombatTaskDocumentRepository"/>.
///
/// <para>
/// Мета: стабілізувати доменно-інфраструктурний контракт:
/// <list type="bullet">
/// <item><description>створення документа як Active з audit полями;</description></item>
/// <item><description>ідемпотентне скасування (Cancel) без повторного перезапису;</description></item>
/// <item><description>фільтри (рік/місяць/статус) + сортування;</description></item>
/// <item><description>валидація параметрів та помилки при відсутності документа.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class CombatTaskDocumentRepositoryTests
{
    //======================================================================
    // CreateAsync
    //======================================================================

    /// <summary>
    /// CreateAsync має створювати документ у статусі Active,
    /// тримати trim для текстових полів і заповнювати audit.
    /// </summary>
    [Fact]
    public async Task CreateAsync_CreatesActiveDocument_WithAudit_AndTrim()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var id = await repo.CreateAsync(
            orderTitle: "  Наказ №1  ",
            recordedAt: new DateOnly(2026, 02, 10),
            description: "  Опис  ",
            author: "tester",
            nowUtc: now);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        var doc = await db.CombatTaskDocuments.SingleAsync(x => x.Id == id);

        Assert.Equal(DocumentStatus.Active, doc.Status);
        Assert.Equal("Наказ №1", doc.OrderTitle);
        Assert.Equal("Опис", doc.Description);
        Assert.Equal(new DateOnly(2026, 02, 10), doc.RecordedAt);

        Assert.Equal("tester", doc.CreatedBy);
        Assert.Equal(now, doc.CreatedAtUtc);
        Assert.Equal("tester", doc.UpdatedBy);
        Assert.Equal(now, doc.UpdatedAtUtc);

        Assert.Null(doc.CanceledBy);
        Assert.Null(doc.CanceledAtUtc);
        Assert.Null(doc.CanceledReason);
    }

    /// <summary>
    /// CreateAsync має відкидати невалідні параметри (мінімальний набір перевірок).
    /// </summary>
    [Fact]
    public async Task CreateAsync_Throws_OnInvalidArguments()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.CreateAsync(orderTitle: " ", recordedAt: new DateOnly(2026, 02, 10), description: null, author: "tester", nowUtc: now));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.CreateAsync(orderTitle: "X", recordedAt: default, description: null, author: "tester", nowUtc: now));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.CreateAsync(orderTitle: "X", recordedAt: new DateOnly(2026, 02, 10), description: null, author: " ", nowUtc: now));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.CreateAsync(orderTitle: "X", recordedAt: new DateOnly(2026, 02, 10), description: null, author: "tester", nowUtc: default));
    }

    /// <summary>
    /// CreateAsync має вимагати UTC для nowUtc.
    /// </summary>
    [Fact]
    public async Task CreateAsync_Throws_WhenNowUtcIsNotUtc()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        var notUtc = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Local);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.CreateAsync(
                orderTitle: "X",
                recordedAt: new DateOnly(2026, 02, 10),
                description: null,
                author: "tester",
                nowUtc: notUtc));
    }

    //======================================================================
    // CancelAsync
    //======================================================================

    /// <summary>
    /// CancelAsync має переводити документ у Canceled, ставити причину та audit.
    /// </summary>
    [Fact]
    public async Task CancelAsync_MarksCanceled_WithReasonAndAudit()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        var createdAt = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var canceledAt = createdAt.AddMinutes(10);

        var id = await repo.CreateAsync(
            orderTitle: "Наказ №2",
            recordedAt: new DateOnly(2026, 02, 11),
            description: null,
            author: "creator",
            nowUtc: createdAt);

        await repo.CancelAsync(
            documentId: id,
            reason: "  Помилка  ",
            author: "auditor",
            nowUtc: canceledAt);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        var doc = await db.CombatTaskDocuments.SingleAsync(x => x.Id == id);

        Assert.Equal(DocumentStatus.Canceled, doc.Status);

        Assert.Equal("auditor", doc.CanceledBy);
        Assert.Equal(canceledAt, doc.CanceledAtUtc);
        Assert.Equal("Помилка", doc.CanceledReason);

        Assert.Equal("auditor", doc.UpdatedBy);
        Assert.Equal(canceledAt, doc.UpdatedAtUtc);
    }

    /// <summary>
    /// CancelAsync має бути ідемпотентним: повторний виклик для вже Canceled документа
    /// не повинен перезаписувати CanceledBy/CanceledReason/Updated*.
    /// </summary>
    [Fact]
    public async Task CancelAsync_IsIdempotent_ForAlreadyCanceledDocument()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        var createdAt = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var firstCancelAt = createdAt.AddMinutes(10);
        var secondCancelAt = createdAt.AddMinutes(20);

        var id = await repo.CreateAsync(
            orderTitle: "Наказ №3",
            recordedAt: new DateOnly(2026, 02, 12),
            description: "D",
            author: "creator",
            nowUtc: createdAt);

        await repo.CancelAsync(
            documentId: id,
            reason: "FIRST",
            author: "auditor1",
            nowUtc: firstCancelAt);

        // Second cancel should be ignored
        await repo.CancelAsync(
            documentId: id,
            reason: "SECOND",
            author: "auditor2",
            nowUtc: secondCancelAt);

        await using var db = await testDb.Factory.CreateDbContextAsync();

        var doc = await db.CombatTaskDocuments.SingleAsync(x => x.Id == id);

        Assert.Equal(DocumentStatus.Canceled, doc.Status);

        Assert.Equal("auditor1", doc.CanceledBy);
        Assert.Equal(firstCancelAt, doc.CanceledAtUtc);
        Assert.Equal("FIRST", doc.CanceledReason);

        Assert.Equal("auditor1", doc.UpdatedBy);
        Assert.Equal(firstCancelAt, doc.UpdatedAtUtc);
    }

    /// <summary>
    /// CancelAsync має кидати помилку, якщо документ не знайдено.
    /// </summary>
    [Fact]
    public async Task CancelAsync_Throws_WhenDocumentNotFound()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CancelAsync(
                documentId: Guid.NewGuid(),
                reason: "X",
                author: "auditor",
                nowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc)));

        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// CancelAsync має вимагати UTC для nowUtc.
    /// </summary>
    [Fact]
    public async Task CancelAsync_Throws_WhenNowUtcIsNotUtc()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        var createdAt = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var id = await repo.CreateAsync(
            orderTitle: "Наказ №UTC",
            recordedAt: new DateOnly(2026, 02, 11),
            description: null,
            author: "creator",
            nowUtc: createdAt);

        var notUtc = new DateTime(2026, 02, 17, 10, 10, 00, DateTimeKind.Unspecified);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.CancelAsync(
                documentId: id,
                reason: "X",
                author: "auditor",
                nowUtc: notUtc));
    }

    //======================================================================
    // GetDocumentsAsync
    //======================================================================

    /// <summary>
    /// GetDocumentsAsync має повертати документи з фільтрами (рік/місяць/статус)
    /// і сортуванням RecordedAt desc, CreatedAtUtc desc.
    /// </summary>
    [Fact]
    public async Task GetDocumentsAsync_FiltersAndSorts_ByRecordedAtAndCreatedAt()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        // Create 3 docs with predictable ordering
        var d1 = await repo.CreateAsync(
            orderTitle: "A",
            recordedAt: new DateOnly(2026, 01, 10),
            description: null,
            author: "u",
            nowUtc: new DateTime(2026, 01, 10, 10, 00, 00, DateTimeKind.Utc));

        var d2 = await repo.CreateAsync(
            orderTitle: "B",
            recordedAt: new DateOnly(2026, 02, 10),
            description: null,
            author: "u",
            nowUtc: new DateTime(2026, 02, 10, 10, 00, 00, DateTimeKind.Utc));

        // Same RecordedAt as d2, later CreatedAtUtc
        var d3 = await repo.CreateAsync(
            orderTitle: "C",
            recordedAt: new DateOnly(2026, 02, 10),
            description: null,
            author: "u",
            nowUtc: new DateTime(2026, 02, 10, 11, 00, 00, DateTimeKind.Utc));

        // Cancel d2 to have different status
        await repo.CancelAsync(
            documentId: d2,
            reason: "R",
            author: "u",
            nowUtc: new DateTime(2026, 02, 10, 12, 00, 00, DateTimeKind.Utc));

        // 1) No filters => sorted: (RecordedAt desc, CreatedAtUtc desc)
        var all = await repo.GetDocumentsAsync(year: null, month: null, status: null, search: null);

        Assert.Equal(3, all.Count);
        Assert.Equal(d3, all[0].Id); // RecordedAt=02/10, CreatedAt later
        Assert.Equal(d2, all[1].Id); // RecordedAt=02/10, CreatedAt earlier
        Assert.Equal(d1, all[2].Id); // RecordedAt=01/10

        // 2) Filter by year/month (2026/02) => d2,d3
        var feb = await repo.GetDocumentsAsync(year: 2026, month: 2, status: null, search: null);
        Assert.Equal(2, feb.Count);
        Assert.Equal(d3, feb[0].Id);
        Assert.Equal(d2, feb[1].Id);

        // 3) Filter by status Active => d1,d3 (d2 canceled)
        var active = await repo.GetDocumentsAsync(year: null, month: null, status: DocumentStatus.Active, search: null);
        Assert.Equal(2, active.Count);
        Assert.Equal(d3, active[0].Id);
        Assert.Equal(d1, active[1].Id);

        // 4) Filter by status Canceled => only d2
        var canceled = await repo.GetDocumentsAsync(year: null, month: null, status: DocumentStatus.Canceled, search: null);
        Assert.Single(canceled);
        Assert.Equal(d2, canceled[0].Id);
    }

    /// <summary>
    /// GetDocumentsAsync має фільтрувати за пошуком по заголовку та опису (trim),
    /// при цьому реалізація повинна коректно працювати і з SQLite (через LIKE fallback).
    /// </summary>
    [Fact]
    public async Task GetDocumentsAsync_Filters_BySearchTitleOrDescription()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        var d1 = await repo.CreateAsync(
            orderTitle: "Alpha",
            recordedAt: new DateOnly(2026, 02, 10),
            description: null,
            author: "u",
            nowUtc: new DateTime(2026, 02, 10, 10, 00, 00, DateTimeKind.Utc));

        var d2 = await repo.CreateAsync(
            orderTitle: "Bravo",
            recordedAt: new DateOnly(2026, 02, 11),
            description: "Needs review",
            author: "u",
            nowUtc: new DateTime(2026, 02, 11, 10, 00, 00, DateTimeKind.Utc));

        var d3 = await repo.CreateAsync(
            orderTitle: "Charlie",
            recordedAt: new DateOnly(2026, 02, 12),
            description: "Other",
            author: "u",
            nowUtc: new DateTime(2026, 02, 12, 10, 00, 00, DateTimeKind.Utc));

        // 1) Search by title
        var byTitle = await repo.GetDocumentsAsync(year: null, month: null, status: null, search: "  brav  ");
        Assert.Single(byTitle);
        Assert.Equal(d2, byTitle[0].Id);

        // 2) Search by description
        var byDesc = await repo.GetDocumentsAsync(year: null, month: null, status: null, search: "review");
        Assert.Single(byDesc);
        Assert.Equal(d2, byDesc[0].Id);

        // 3) Search with no matches
        var none = await repo.GetDocumentsAsync(year: null, month: null, status: null, search: "zzz");
        Assert.Empty(none);

        _ = d1;
        _ = d3;
    }

    /// <summary>
    /// GetDocumentsAsync має валідовувати діапазон місяця.
    /// </summary>
    [Fact]
    public async Task GetDocumentsAsync_Throws_WhenMonthOutOfRange()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repo.GetDocumentsAsync(year: 2026, month: 0, status: null, search: null));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repo.GetDocumentsAsync(year: 2026, month: 13, status: null, search: null));
    }
}
