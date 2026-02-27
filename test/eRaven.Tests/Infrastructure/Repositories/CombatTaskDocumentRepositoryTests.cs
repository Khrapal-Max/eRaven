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

public sealed class CombatTaskDocumentRepositoryTests
{
    [Fact]
    public async Task CreateAsync_PersistsActiveDocument_AndTrimsFields()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        var nowUtc = new DateTime(2026, 02, 10, 10, 00, 00, DateTimeKind.Utc);
        var recordedAt = new DateOnly(2026, 02, 10);

        var id = await repo.CreateAsync(
            orderTitle: "  Наказ №1  ",
            recordedAt: recordedAt,
            description: "  опис  ",
            author: "  admin  ",
            nowUtc: nowUtc);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var doc = await db.CombatTaskDocuments.SingleAsync(x => x.Id == id);

        Assert.Equal(DocumentStatus.Active, doc.Status);
        Assert.Equal("Наказ №1", doc.OrderTitle);
        Assert.Equal("опис", doc.Description);
        Assert.Equal(recordedAt, doc.RecordedAt);

        Assert.Equal("admin", doc.CreatedBy);
        Assert.Equal(nowUtc, doc.CreatedAtUtc);
        Assert.Equal("admin", doc.UpdatedBy);
        Assert.Equal(nowUtc, doc.UpdatedAtUtc);

        Assert.Null(doc.CanceledBy);
        Assert.Null(doc.CanceledAtUtc);
        Assert.Null(doc.CanceledReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAsync_Throws_WhenAuthorMissing(string? author)
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.CreateAsync(
                orderTitle: "Order",
                recordedAt: new DateOnly(2026, 02, 10),
                description: null,
                author: author!,
                nowUtc: new DateTime(2026, 02, 10, 10, 00, 00, DateTimeKind.Utc)));

        Assert.Contains("author", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenNowUtcNotUtc()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.CreateAsync(
                orderTitle: "Order",
                recordedAt: new DateOnly(2026, 02, 10),
                description: null,
                author: "admin",
                nowUtc: new DateTime(2026, 02, 10, 10, 00, 00, DateTimeKind.Local)));

        Assert.Contains("UTC", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelAsync_MarksDocumentCanceled_AndIsIdempotent()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        var createdAtUtc = new DateTime(2026, 02, 10, 10, 00, 00, DateTimeKind.Utc);
        var canceledAtUtc = new DateTime(2026, 02, 11, 09, 00, 00, DateTimeKind.Utc);

        var id = await repo.CreateAsync(
            orderTitle: "Order",
            recordedAt: new DateOnly(2026, 02, 10),
            description: null,
            author: "author",
            nowUtc: createdAtUtc);

        await repo.CancelAsync(
            documentId: id,
            reason: "  помилка  ",
            author: "  canceler  ",
            nowUtc: canceledAtUtc);

        // Second cancel should be idempotent and must NOT overwrite reason/timestamps.
        var canceledAtUtc2 = new DateTime(2026, 02, 12, 09, 00, 00, DateTimeKind.Utc);
        await repo.CancelAsync(
            documentId: id,
            reason: "інша причина",
            author: "someone",
            nowUtc: canceledAtUtc2);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var doc = await db.CombatTaskDocuments.SingleAsync(x => x.Id == id);

        Assert.Equal(DocumentStatus.Canceled, doc.Status);
        Assert.Equal("canceler", doc.CanceledBy);
        Assert.Equal(canceledAtUtc, doc.CanceledAtUtc);
        Assert.Equal("помилка", doc.CanceledReason);

        // Updated* should reflect the first cancel (second is no-op).
        Assert.Equal("canceler", doc.UpdatedBy);
        Assert.Equal(canceledAtUtc, doc.UpdatedAtUtc);

        // Created stays intact.
        Assert.Equal("author", doc.CreatedBy);
        Assert.Equal(createdAtUtc, doc.CreatedAtUtc);
    }

    [Fact]
    public async Task CancelAsync_Throws_WhenDocumentNotFound()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CancelAsync(
                documentId: Guid.NewGuid(),
                reason: null,
                author: "admin",
                nowUtc: new DateTime(2026, 02, 10, 10, 00, 00, DateTimeKind.Utc)));

        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDocumentsAsync_FiltersSearchesAndOrders()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        // Same RecordedAt, different CreatedAtUtc -> order by CreatedAtUtc desc.
        var id1 = await repo.CreateAsync(
            orderTitle: "Order Alpha",
            recordedAt: new DateOnly(2026, 02, 10),
            description: "first note",
            author: "a",
            nowUtc: new DateTime(2026, 02, 10, 10, 00, 00, DateTimeKind.Utc));

        var id2 = await repo.CreateAsync(
            orderTitle: "Order Beta",
            recordedAt: new DateOnly(2026, 02, 10),
            description: null,
            author: "a",
            nowUtc: new DateTime(2026, 02, 10, 12, 00, 00, DateTimeKind.Utc));

        var id3 = await repo.CreateAsync(
            orderTitle: "Gamma",
            recordedAt: new DateOnly(2026, 01, 05),
            description: "something",
            author: "a",
            nowUtc: new DateTime(2026, 01, 05, 09, 00, 00, DateTimeKind.Utc));

        // Cancel id2
        await repo.CancelAsync(
            documentId: id2,
            reason: "reason",
            author: "a",
            nowUtc: new DateTime(2026, 02, 11, 09, 00, 00, DateTimeKind.Utc));

        // No filters: should return [id2, id1, id3]
        var all = await repo.GetDocumentsAsync(year: null, month: null, status: null, search: null);
        Assert.Equal([id2, id1, id3], [.. all.Select(x => x.Id)]);

        // Filter active February 2026: should return only id1
        var filtered = await repo.GetDocumentsAsync(
            year: 2026,
            month: 2,
            status: DocumentStatus.Active,
            search: null);
        Assert.Single(filtered);
        Assert.Equal(id1, filtered[0].Id);

        // Search by title
        var byTitle = await repo.GetDocumentsAsync(
            year: null,
            month: null,
            status: null,
            search: "Alpha");
        Assert.Single(byTitle);
        Assert.Equal(id1, byTitle[0].Id);

        // Search by description (SQLite LIKE)
        var byDesc = await repo.GetDocumentsAsync(
            year: null,
            month: null,
            status: null,
            search: "first");
        Assert.Single(byDesc);
        Assert.Equal(id1, byDesc[0].Id);
    }

    [Theory]
    [InlineData(1899, null)]
    [InlineData(2101, null)]
    [InlineData(null, 0)]
    [InlineData(null, 13)]
    public async Task GetDocumentsAsync_Throws_OnInvalidYearOrMonth(int? year, int? month)
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(testDb.Factory);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repo.GetDocumentsAsync(year: year, month: month, status: null, search: null));
    }
}
