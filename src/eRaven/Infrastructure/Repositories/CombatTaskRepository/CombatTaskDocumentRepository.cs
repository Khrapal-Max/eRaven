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

/// <summary>
/// EF Core repository for <see cref="CombatTaskDocument"/>.
/// </summary>
public sealed class CombatTaskDocumentRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ICombatTaskDocumentRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <inheritdoc />
    public async Task<IReadOnlyList<CombatTaskDocumentDto>> GetDocumentsAsync(
        int? year,
        int? month,
        DocumentStatus? status,
        string? search,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.CombatTaskDocuments
            .AsNoTracking();

        if (year.HasValue)
            q = q.Where(x => x.RecordedAt.Year == year.Value);

        if (month.HasValue)
            q = q.Where(x => x.RecordedAt.Month == month.Value);

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x =>
                EF.Functions.ILike(x.OrderTitle, $"%{s}%")
                || (x.Description != null && EF.Functions.ILike(x.Description, $"%{s}%")));
        }

        return await q
            .OrderByDescending(x => x.RecordedAt)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new CombatTaskDocumentDto(
                DocumentId: x.Id,
                OrderTitle: x.OrderTitle,
                Description: x.Description,
                Status: x.Status,
                RecordedAt: x.RecordedAt,
                CanceledReason: x.CanceledReason ?? string.Empty))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(
        string orderTitle,
        DateOnly recordedAt,
        string? description,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orderTitle))
            throw new ArgumentException("Order title is required.", nameof(orderTitle));

        if (recordedAt == default)
            throw new ArgumentException("RecordedAt must be set.", nameof(recordedAt));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("author is required.", nameof(author));

        if (nowUtc == default)
            throw new ArgumentException("nowUtc must be set.", nameof(nowUtc));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = new CombatTaskDocument
        {
            Id = Guid.NewGuid(),
            Status = DocumentStatus.Active,
            OrderTitle = orderTitle.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            RecordedAt = recordedAt,
            CreatedBy = author,
            CreatedAtUtc = nowUtc,
            UpdatedBy = author,
            UpdatedAtUtc = nowUtc
        };

        db.CombatTaskDocuments.Add(doc);
        await db.SaveChangesAsync(ct);
        return doc.Id;
    }

    /// <inheritdoc />
    public async Task CancelAsync(
        Guid documentId,
        string? reason,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (documentId == default)
            throw new ArgumentException("documentId must be set.", nameof(documentId));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("author is required.", nameof(author));

        if (nowUtc == default)
            throw new ArgumentException("nowUtc must be set.", nameof(nowUtc));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CombatTaskDocuments
            .SingleOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("CombatTaskDocument not found.");

        if (doc.Status == DocumentStatus.Canceled)
            return; // idempotent

        doc.Status = DocumentStatus.Canceled;
        doc.CanceledBy = author;
        doc.CanceledAtUtc = nowUtc;
        doc.CanceledReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        doc.UpdatedBy = author;
        doc.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
    }
}
