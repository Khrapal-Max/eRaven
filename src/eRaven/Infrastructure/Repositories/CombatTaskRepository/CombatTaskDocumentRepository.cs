//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDocumentRepository
//-----------------------------------------------------------------------------


using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

// TODO need tests
public class CombatTaskDocumentRepository(IDbContextFactory<AppDbContext> dbFactory) : ICombatTaskDocumentRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<IReadOnlyList<CombatTaskDocumentDto>> GetDocumentsAsync(int year, int month, DocumentStatus? status, string? search, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1);

        var query = db.CombatTaskDocuments
           .AsNoTracking()
           .Where(d => d.RecordedAt >= from && d.RecordedAt < to);

        if (status is not null)
            query = query.Where(d => d.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(d => d.DocumentTitle.Contains(search));
        }

        return await query
            .Select(d => new CombatTaskDocumentDto
            (
                DocumentId: d.Id,
                Title: d.DocumentTitle,
                Status: d.Status,
                Order: d.Order ?? string.Empty,
                RecordedAt: d.RecordedAt,
                CanceledReason: d.CanceledReason ?? string.Empty
            ))
            .ToListAsync(ct);
    }

    public async Task<Guid> CreateDraftAsync(string documentTitle, DateOnly recordedAt, string author, DateTime nowUtc, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var document = new CombatTaskDocument
        {
            Id = Guid.NewGuid(),
            DocumentTitle = documentTitle,
            RecordedAt = recordedAt,
            Status = DocumentStatus.Draft,
            CreatedBy = author,
            CreatedAtUtc = nowUtc
        };

        db.CombatTaskDocuments.Add(document);
        await db.SaveChangesAsync(ct);

        return document.Id;
    }

    public async Task PostAsync(Guid documentId, string order, string author, DateTime nowUtc, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("DocumentId is required.", nameof(documentId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var document = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (document.Status == DocumentStatus.Posted)
            return; // ідемпотентність (щоб не дублювати)

        if (document.Status == DocumentStatus.Canceled)
            throw new InvalidOperationException("Документ відмінений і не може бути проведений.");

        document.Status = DocumentStatus.Posted;
        document.Order = order;
        document.UpdatedBy = author;
        document.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(Guid documentId, string reason, string author, DateTime nowUtc, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("DocumentId is required.", nameof(documentId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var document = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (document.Status == DocumentStatus.Posted)
            throw new InvalidOperationException("Документ вже проведений.");

        if (document.Status == DocumentStatus.Canceled)
            return; // ідемпотентність (щоб не дублювати)

        document.Status = DocumentStatus.Canceled;
        document.CanceledReason = reason;
        document.CreatedBy = author;
        document.CanceledAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
    }
}
