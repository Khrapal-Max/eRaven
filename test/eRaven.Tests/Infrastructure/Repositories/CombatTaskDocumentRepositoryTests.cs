//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDocumentRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class CombatTaskDocumentRepositoryTests
{
    private static readonly DateTime NowUtc = new(2026, 02, 15, 12, 0, 0, DateTimeKind.Utc);

    //======================================================================
    // Seed helpers
    //======================================================================

    private static CombatTaskDocument NewDoc(
        string title,
        DateOnly recordedAt,
        DocumentStatus status,
        string createdBy = "seed",
        string? description = null)
        => new()
        {
            Id = Guid.NewGuid(),
            OrderTitle = title,
            Description = description,
            RecordedAt = recordedAt,
            Status = status,
            CreatedBy = createdBy,
            CreatedAtUtc = NowUtc.AddDays(-1)
        };

    //======================================================================
    // GetDocumentsAsync
    //======================================================================

    [Fact]
    public async Task GetDocumentsAsync_returns_only_documents_in_requested_month_and_maps_canceled_reason_to_empty_when_null()
    {
        await using var tdb = new SqliteTestDb();

        var feb01 = new DateOnly(2026, 2, 1);
        var feb28 = new DateOnly(2026, 2, 28);
        var mar01 = new DateOnly(2026, 3, 1);

        var d1 = NewDoc("OPORD-001", feb01, DocumentStatus.Draft);
        d1.CanceledReason = null; // перевіряємо мапінг на string.Empty

        var d2 = NewDoc("OPORD-002", feb28, DocumentStatus.Posted);
        var d3 = NewDoc("OPORD-OUT", mar01, DocumentStatus.Draft); // поза діапазоном (RecordedAt < to)

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.AddRange(d1, d2, d3);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskDocumentRepository(tdb.Factory);

        var list = await repo.GetDocumentsAsync(2026, 2, status: null, search: null);

        Assert.Equal(2, list.Count);
        Assert.Contains(list, x => x.DocumentId == d1.Id);
        Assert.Contains(list, x => x.DocumentId == d2.Id);
        Assert.DoesNotContain(list, x => x.DocumentId == d3.Id);

        var dto1 = list.Single(x => x.DocumentId == d1.Id);
        Assert.Equal(string.Empty, dto1.CanceledReason);
    }

    [Fact]
    public async Task GetDocumentsAsync_filters_by_status_when_specified()
    {
        await using var tdb = new SqliteTestDb();

        var d1 = NewDoc("DOC-DRAFT", new DateOnly(2026, 2, 10), DocumentStatus.Draft);
        var d2 = NewDoc("DOC-POSTED", new DateOnly(2026, 2, 11), DocumentStatus.Posted);
        var d3 = NewDoc("DOC-CANCELED", new DateOnly(2026, 2, 12), DocumentStatus.Canceled);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.AddRange(d1, d2, d3);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskDocumentRepository(tdb.Factory);

        var list = await repo.GetDocumentsAsync(2026, 2, status: DocumentStatus.Posted, search: null);

        Assert.Single(list);
        Assert.Equal(d2.Id, list[0].DocumentId);
        Assert.Equal(DocumentStatus.Posted, list[0].Status);
    }

    [Fact]
    public async Task GetDocumentsAsync_filters_by_search_in_order_title()
    {
        await using var tdb = new SqliteTestDb();

        var d1 = NewDoc("OPORD-AAA", new DateOnly(2026, 2, 10), DocumentStatus.Draft);
        var d2 = NewDoc("SOME-OTHER", new DateOnly(2026, 2, 11), DocumentStatus.Draft);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.AddRange(d1, d2);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskDocumentRepository(tdb.Factory);

        var list = await repo.GetDocumentsAsync(2026, 2, status: null, search: "OPORD");

        Assert.Single(list);
        Assert.Equal(d1.Id, list[0].DocumentId);
        Assert.Equal("OPORD-AAA", list[0].OrderTitle);
    }

    //======================================================================
    // CreateDraftAsync
    //======================================================================

    [Fact]
    public async Task CreateDraftAsync_creates_document_with_draft_status_and_audit_fields()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(tdb.Factory);

        var id = await repo.CreateDraftAsync(
            orderTitle: "OPORD-NEW",
            recordedAt: new DateOnly(2026, 2, 20),
            description: "desc",
            author: "user1",
            nowUtc: NowUtc);

        Assert.NotEqual(Guid.Empty, id);

        await using var db = await tdb.Factory.CreateDbContextAsync();
        var doc = await db.CombatTaskDocuments.AsNoTracking().SingleAsync(x => x.Id == id);

        Assert.Equal(DocumentStatus.Draft, doc.Status);
        Assert.Equal("OPORD-NEW", doc.OrderTitle);
        Assert.Equal("desc", doc.Description);
        Assert.Equal(new DateOnly(2026, 2, 20), doc.RecordedAt);
        Assert.Equal("user1", doc.CreatedBy);
        Assert.Equal(NowUtc, doc.CreatedAtUtc);
        Assert.Null(doc.UpdatedBy);
        Assert.Null(doc.UpdatedAtUtc);
        Assert.Null(doc.CanceledBy);
        Assert.Null(doc.CanceledAtUtc);
        Assert.Null(doc.CanceledReason);
    }

    //======================================================================
    // PostAsync
    //======================================================================

    [Fact]
    public async Task PostAsync_throws_on_empty_document_id()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.PostAsync(Guid.Empty, author: "u", nowUtc: NowUtc));
    }

    [Fact]
    public async Task PostAsync_throws_when_document_not_found()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.PostAsync(Guid.NewGuid(), author: "u", nowUtc: NowUtc));

        Assert.Equal("Документ не знайдено.", ex.Message);
    }

    [Fact]
    public async Task PostAsync_when_document_is_canceled_should_throw()
    {
        await using var tdb = new SqliteTestDb();
        var doc = NewDoc("DOC", new DateOnly(2026, 2, 10), DocumentStatus.Canceled);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskDocumentRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.PostAsync(doc.Id, author: "u", nowUtc: NowUtc));

        Assert.Equal("Документ відмінений і не може бути проведений.", ex.Message);
    }

    [Fact]
    public async Task PostAsync_when_document_is_draft_should_set_posted_and_update_audit_fields()
    {
        await using var tdb = new SqliteTestDb();
        var doc = NewDoc("DOC", new DateOnly(2026, 2, 10), DocumentStatus.Draft);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskDocumentRepository(tdb.Factory);

        await repo.PostAsync(doc.Id, author: "poster", nowUtc: NowUtc);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.CombatTaskDocuments.AsNoTracking().SingleAsync(x => x.Id == doc.Id);

        Assert.Equal(DocumentStatus.Posted, reloaded.Status);
        Assert.Equal("poster", reloaded.UpdatedBy);
        Assert.Equal(NowUtc, reloaded.UpdatedAtUtc);
    }

    [Fact]
    public async Task PostAsync_when_document_is_already_posted_should_noop_and_not_override_audit_fields()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("DOC", new DateOnly(2026, 2, 10), DocumentStatus.Posted);
        doc.UpdatedBy = "seed";
        doc.UpdatedAtUtc = NowUtc.AddHours(-3);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskDocumentRepository(tdb.Factory);
        await repo.PostAsync(doc.Id, author: "new", nowUtc: NowUtc);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.CombatTaskDocuments.AsNoTracking().SingleAsync(x => x.Id == doc.Id);

        Assert.Equal(DocumentStatus.Posted, reloaded.Status);
        Assert.Equal("seed", reloaded.UpdatedBy);
        Assert.Equal(NowUtc.AddHours(-3), reloaded.UpdatedAtUtc);
    }

    //======================================================================
    // CancelAsync
    //======================================================================

    [Fact]
    public async Task CancelAsync_throws_on_empty_document_id()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.CancelAsync(Guid.Empty, reason: "r", author: "u", nowUtc: NowUtc));
    }

    [Fact]
    public async Task CancelAsync_throws_when_document_not_found()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskDocumentRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CancelAsync(Guid.NewGuid(), reason: "r", author: "u", nowUtc: NowUtc));

        Assert.Equal("Документ не знайдено.", ex.Message);
    }

    [Fact]
    public async Task CancelAsync_when_document_is_posted_should_throw()
    {
        await using var tdb = new SqliteTestDb();
        var doc = NewDoc("DOC", new DateOnly(2026, 2, 10), DocumentStatus.Posted);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskDocumentRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CancelAsync(doc.Id, reason: "r", author: "u", nowUtc: NowUtc));

        Assert.Equal("Документ вже проведений.", ex.Message);
    }

    [Fact]
    public async Task CancelAsync_when_document_is_draft_should_set_canceled_and_audit_fields()
    {
        await using var tdb = new SqliteTestDb();
        var doc = NewDoc("DOC", new DateOnly(2026, 2, 10), DocumentStatus.Draft);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskDocumentRepository(tdb.Factory);
        await repo.CancelAsync(doc.Id, reason: "because", author: "canceler", nowUtc: NowUtc);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.CombatTaskDocuments.AsNoTracking().SingleAsync(x => x.Id == doc.Id);

        Assert.Equal(DocumentStatus.Canceled, reloaded.Status);
        Assert.Equal("because", reloaded.CanceledReason);
        Assert.Equal("canceler", reloaded.CanceledBy);
        Assert.Equal(NowUtc, reloaded.CanceledAtUtc);
    }

    [Fact]
    public async Task CancelAsync_when_document_is_already_canceled_should_noop_and_not_override_reason_or_audit_fields()
    {
        await using var tdb = new SqliteTestDb();

        var doc = NewDoc("DOC", new DateOnly(2026, 2, 10), DocumentStatus.Canceled);
        doc.CanceledReason = "r1";
        doc.CanceledBy = "seed";
        doc.CanceledAtUtc = NowUtc.AddHours(-2);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.CombatTaskDocuments.Add(doc);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskDocumentRepository(tdb.Factory);
        await repo.CancelAsync(doc.Id, reason: "r2", author: "new", nowUtc: NowUtc);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.CombatTaskDocuments.AsNoTracking().SingleAsync(x => x.Id == doc.Id);

        Assert.Equal(DocumentStatus.Canceled, reloaded.Status);
        Assert.Equal("r1", reloaded.CanceledReason);
        Assert.Equal("seed", reloaded.CanceledBy);
        Assert.Equal(NowUtc.AddHours(-2), reloaded.CanceledAtUtc);
    }
}
